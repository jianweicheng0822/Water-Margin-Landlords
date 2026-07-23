using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates and identifies card combinations in Dou Di Zhu.
/// Call Evaluate() with a list of selected cards to get a CardCombo result.
/// </summary>
public static class CardRules
{
    /// <summary>
    /// Evaluates a list of cards and returns the corresponding CardCombo.
    /// Returns null if the cards do not form a valid combination.
    /// </summary>
    public static CardCombo Evaluate(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return null;

        // Build a dictionary: rank -> count of cards with that rank
        Dictionary<Rank, int> rankCount = GetRankCount(cards);
        int count = cards.Count;

        // Try to match from simplest to most complex
        CardCombo result;

        // Rocket: Black Joker + Red Joker
        result = TryRocket(cards, rankCount, count);
        if (result != null) return result;

        // Bomb: four cards of the same rank
        result = TryBomb(cards, rankCount, count);
        if (result != null) return result;

        // Single card
        result = TrySingle(cards, rankCount, count);
        if (result != null) return result;

        // Pair
        result = TryPair(cards, rankCount, count);
        if (result != null) return result;

        // Triple / Triple with single / Triple with pair
        result = TryTriple(cards, rankCount, count);
        if (result != null) return result;

        // Straight (5+ consecutive singles)
        result = TryStraight(cards, rankCount, count);
        if (result != null) return result;

        // Straight pair (3+ consecutive pairs)
        result = TryStraightPair(cards, rankCount, count);
        if (result != null) return result;

        // Plane (consecutive triples, with or without kickers)
        result = TryPlane(cards, rankCount, count);
        if (result != null) return result;

        // Four with two singles / two pairs
        result = TryFourWithTwo(cards, rankCount, count);
        if (result != null) return result;

        return null;
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Builds a rank -> count dictionary from a list of cards.
    /// </summary>
    private static Dictionary<Rank, int> GetRankCount(List<Card> cards)
    {
        Dictionary<Rank, int> rankCount = new Dictionary<Rank, int>();
        foreach (Card card in cards)
        {
            if (rankCount.ContainsKey(card.Rank))
                rankCount[card.Rank]++;
            else
                rankCount[card.Rank] = 1;
        }
        return rankCount;
    }

    /// <summary>
    /// Checks if ranks form a consecutive sequence (no 2 or jokers allowed).
    /// </summary>
    private static bool IsConsecutive(List<Rank> sortedRanks)
    {
        for (int i = 1; i < sortedRanks.Count; i++)
        {
            if ((int)sortedRanks[i] - (int)sortedRanks[i - 1] != 1)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if the rank can be part of a straight (3 through Ace, no 2 or jokers).
    /// </summary>
    private static bool IsStraightRank(Rank rank)
    {
        return rank >= Rank.Three && rank <= Rank.Ace;
    }

    // ==================== Combo Detection ====================

    /// <summary>
    /// Rocket: exactly Black Joker + Red Joker.
    /// </summary>
    private static CardCombo TryRocket(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count == 2 && rankCount.ContainsKey(Rank.BlackJoker) && rankCount.ContainsKey(Rank.RedJoker))
            return new CardCombo(ComboType.Rocket, Rank.RedJoker, cards);
        return null;
    }

    /// <summary>
    /// Bomb: exactly 4 cards of the same rank.
    /// </summary>
    private static CardCombo TryBomb(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count == 4 && rankCount.Count == 1)
        {
            Rank rank = rankCount.Keys.First();
            return new CardCombo(ComboType.Bomb, rank, cards);
        }
        return null;
    }

    /// <summary>
    /// Single: exactly 1 card.
    /// </summary>
    private static CardCombo TrySingle(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count == 1)
            return new CardCombo(ComboType.Single, cards[0].Rank, cards);
        return null;
    }

    /// <summary>
    /// Pair: exactly 2 cards of the same rank (not jokers).
    /// </summary>
    private static CardCombo TryPair(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count == 2 && rankCount.Count == 1 && !cards[0].IsJoker)
            return new CardCombo(ComboType.Pair, cards[0].Rank, cards);
        return null;
    }

    /// <summary>
    /// Triple: 3 cards of same rank.
    /// Triple with single: 3 + 1 kicker.
    /// Triple with pair: 3 + 2 kicker of same rank.
    /// </summary>
    private static CardCombo TryTriple(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        // Find the rank that appears 3 times
        Rank? tripleRank = null;
        foreach (var kv in rankCount)
        {
            if (kv.Value == 3)
            {
                tripleRank = kv.Key;
                break;
            }
        }

        if (tripleRank == null)
            return null;

        // Pure triple
        if (count == 3)
            return new CardCombo(ComboType.Triple, tripleRank.Value, cards);

        // Triple with single
        if (count == 4 && rankCount.Count == 2)
            return new CardCombo(ComboType.TripleWithSingle, tripleRank.Value, cards);

        // Triple with pair (kicker must be a pair, not jokers)
        if (count == 5 && rankCount.Count == 2)
        {
            foreach (var kv in rankCount)
            {
                if (kv.Key != tripleRank && kv.Value == 2)
                    return new CardCombo(ComboType.TripleWithPair, tripleRank.Value, cards);
            }
        }

        return null;
    }

    /// <summary>
    /// Straight: 5 or more consecutive single cards (3 to Ace only, no 2 or jokers).
    /// </summary>
    private static CardCombo TryStraight(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count < 5 || count > 12)
            return null;

        // Every rank must appear exactly once
        if (rankCount.Count != count)
            return null;

        // All ranks must be valid for straights
        List<Rank> ranks = rankCount.Keys.ToList();
        if (ranks.Any(r => !IsStraightRank(r)))
            return null;

        ranks.Sort();

        if (!IsConsecutive(ranks))
            return null;

        return new CardCombo(ComboType.Straight, ranks[ranks.Count - 1], cards);
    }

    /// <summary>
    /// Straight pair: 3 or more consecutive pairs (3 to Ace only).
    /// e.g. 334455 (6 cards), 33445566 (8 cards).
    /// </summary>
    private static CardCombo TryStraightPair(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        if (count < 6 || count % 2 != 0)
            return null;

        int pairCount = count / 2;
        if (pairCount < 3)
            return null;

        // Every rank must appear exactly twice
        if (rankCount.Count != pairCount)
            return null;

        if (rankCount.Values.Any(v => v != 2))
            return null;

        List<Rank> ranks = rankCount.Keys.ToList();
        if (ranks.Any(r => !IsStraightRank(r)))
            return null;

        ranks.Sort();

        if (!IsConsecutive(ranks))
            return null;

        return new CardCombo(ComboType.StraightPair, ranks[ranks.Count - 1], cards);
    }

    /// <summary>
    /// Plane: 2+ consecutive triples, optionally with single or pair kickers.
    /// Plane (no kickers): e.g. 333444
    /// Plane with singles: e.g. 333444 + 5 + 6
    /// Plane with pairs: e.g. 333444 + 55 + 66
    /// </summary>
    private static CardCombo TryPlane(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        // Find all ranks that appear 3 or more times (treat 4 as 3 + 1 kicker)
        List<Rank> tripleRanks = new List<Rank>();
        foreach (var kv in rankCount)
        {
            if (kv.Value >= 3 && IsStraightRank(kv.Key))
                tripleRanks.Add(kv.Key);
        }

        if (tripleRanks.Count < 2)
            return null;

        tripleRanks.Sort();

        // Find the longest consecutive triple sequence
        List<Rank> bestSequence = FindLongestConsecutive(tripleRanks);

        if (bestSequence.Count < 2)
            return null;

        int tripleCount = bestSequence.Count;
        int tripleCards = tripleCount * 3;
        int kickerCards = count - tripleCards;

        // Pure plane (no kickers)
        if (kickerCards == 0)
            return new CardCombo(ComboType.Plane, bestSequence[bestSequence.Count - 1], cards);

        // Plane with singles: one single kicker per triple
        if (kickerCards == tripleCount)
            return new CardCombo(ComboType.PlaneWithSingles, bestSequence[bestSequence.Count - 1], cards);

        // Plane with pairs: one pair kicker per triple
        if (kickerCards == tripleCount * 2)
        {
            // Verify the remaining cards form valid pairs
            int pairKickerCount = 0;
            foreach (var kv in rankCount)
            {
                if (bestSequence.Contains(kv.Key))
                {
                    // Extra cards from triple ranks count as kickers
                    if (kv.Value > 3)
                        pairKickerCount += (kv.Value - 3) / 2;
                }
                else
                {
                    if (kv.Value % 2 == 0)
                        pairKickerCount += kv.Value / 2;
                    else
                        return null; // Odd count in kicker means invalid
                }
            }

            if (pairKickerCount == tripleCount)
                return new CardCombo(ComboType.PlaneWithPairs, bestSequence[bestSequence.Count - 1], cards);
        }

        return null;
    }

    /// <summary>
    /// Four with two: 4 of a kind + 2 single kickers or 2 pair kickers.
    /// Note: this is NOT a bomb; it does not beat other combo types.
    /// </summary>
    private static CardCombo TryFourWithTwo(List<Card> cards, Dictionary<Rank, int> rankCount, int count)
    {
        // Find the rank that appears 4 times
        Rank? quadRank = null;
        foreach (var kv in rankCount)
        {
            if (kv.Value == 4)
            {
                quadRank = kv.Key;
                break;
            }
        }

        if (quadRank == null)
            return null;

        // Four with two singles (6 cards total)
        if (count == 6)
            return new CardCombo(ComboType.FourWithTwo, quadRank.Value, cards);

        // Four with two pairs (8 cards total, kickers must be pairs)
        if (count == 8)
        {
            bool validPairs = true;
            foreach (var kv in rankCount)
            {
                if (kv.Key == quadRank)
                    continue;
                if (kv.Value != 2)
                {
                    validPairs = false;
                    break;
                }
            }

            if (validPairs)
                return new CardCombo(ComboType.FourWithTwoPairs, quadRank.Value, cards);
        }

        return null;
    }

    /// <summary>
    /// Finds the longest consecutive sub-sequence from a sorted list of ranks.
    /// </summary>
    private static List<Rank> FindLongestConsecutive(List<Rank> sortedRanks)
    {
        List<Rank> best = new List<Rank>();
        List<Rank> current = new List<Rank> { sortedRanks[0] };

        for (int i = 1; i < sortedRanks.Count; i++)
        {
            if ((int)sortedRanks[i] - (int)sortedRanks[i - 1] == 1)
            {
                current.Add(sortedRanks[i]);
            }
            else
            {
                if (current.Count > best.Count)
                    best = new List<Rank>(current);
                current = new List<Rank> { sortedRanks[i] };
            }
        }

        if (current.Count > best.Count)
            best = current;

        return best;
    }
}
