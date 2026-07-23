using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI strategy for Dou Di Zhu. Handles hand evaluation, hand decomposition,
/// and play decisions. Designed as a static utility so it can be tested independently.
/// </summary>
public static class AIStrategy
{
    // Minimum score threshold to bid for landlord
    private const int BID_THRESHOLD = 7;

    // ==================== Bidding ====================

    /// <summary>
    /// Evaluates a hand and returns a score indicating its strength.
    /// Higher score = stronger hand = more likely to bid landlord.
    /// </summary>
    public static int EvaluateHand(List<Card> hand)
    {
        int score = 0;

        // Count rank occurrences
        Dictionary<Rank, int> rankCount = new Dictionary<Rank, int>();
        foreach (Card card in hand)
        {
            if (rankCount.ContainsKey(card.Rank))
                rankCount[card.Rank]++;
            else
                rankCount[card.Rank] = 1;
        }

        // Rocket (both jokers): +8
        if (rankCount.ContainsKey(Rank.BlackJoker) && rankCount.ContainsKey(Rank.RedJoker))
            score += 8;
        else
        {
            // Individual jokers
            if (rankCount.ContainsKey(Rank.RedJoker)) score += 3;
            if (rankCount.ContainsKey(Rank.BlackJoker)) score += 2;
        }

        // Bombs (four of a kind): +6 each
        foreach (var kv in rankCount)
        {
            if (kv.Value == 4)
                score += 6;
        }

        // Each 2: +2
        if (rankCount.ContainsKey(Rank.Two))
            score += rankCount[Rank.Two] * 2;

        // Each Ace: +1
        if (rankCount.ContainsKey(Rank.Ace))
            score += rankCount[Rank.Ace];

        return score;
    }

    /// <summary>
    /// Returns true if the AI should bid for landlord based on hand strength.
    /// </summary>
    public static bool ShouldBid(List<Card> hand)
    {
        return EvaluateHand(hand) >= BID_THRESHOLD;
    }

    // ==================== Hand Decomposition ====================

    /// <summary>
    /// Represents a group of cards with the same rank in a hand.
    /// Used internally for hand decomposition.
    /// </summary>
    public class RankGroup
    {
        public Rank Rank;
        public int Count;
        public List<Card> Cards;

        public RankGroup(Rank rank, List<Card> cards)
        {
            Rank = rank;
            Count = cards.Count;
            Cards = new List<Card>(cards);
        }
    }

    /// <summary>
    /// Groups hand cards by rank, sorted by rank ascending.
    /// </summary>
    public static List<RankGroup> GroupByRank(List<Card> hand)
    {
        Dictionary<Rank, List<Card>> groups = new Dictionary<Rank, List<Card>>();
        foreach (Card card in hand)
        {
            if (!groups.ContainsKey(card.Rank))
                groups[card.Rank] = new List<Card>();
            groups[card.Rank].Add(card);
        }

        List<RankGroup> result = new List<RankGroup>();
        foreach (var kv in groups)
        {
            result.Add(new RankGroup(kv.Key, kv.Value));
        }

        // Sort by rank ascending (play small cards first)
        result.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        return result;
    }

    /// <summary>
    /// Decomposes a hand into a list of playable combos.
    /// Tries to form the best combinations to minimize leftover singles.
    /// Priority: straights/sequences first, then triples, pairs, singles.
    /// </summary>
    public static List<CardCombo> DecomposeHand(List<Card> hand)
    {
        List<CardCombo> combos = new List<CardCombo>();

        // Work on a copy to avoid modifying the original hand
        List<Card> remaining = new List<Card>(hand);

        // Step 1: Extract rockets
        ExtractRockets(remaining, combos);

        // Step 2: Extract bombs
        ExtractBombs(remaining, combos);

        // Step 3: Extract straights (longest first)
        ExtractStraights(remaining, combos);

        // Step 4: Extract straight pairs (consecutive pairs)
        ExtractStraightPairs(remaining, combos);

        // Step 5: Extract planes (consecutive triples)
        ExtractPlanes(remaining, combos);

        // Step 6: Extract triples (with kickers from remaining singles/pairs)
        ExtractTriples(remaining, combos);

        // Step 7: Extract pairs
        ExtractPairs(remaining, combos);

        // Step 8: Remaining cards become singles
        ExtractSingles(remaining, combos);

        return combos;
    }

    /// <summary>
    /// Extracts rocket (both jokers) from remaining cards.
    /// </summary>
    private static void ExtractRockets(List<Card> remaining, List<CardCombo> combos)
    {
        Card blackJoker = remaining.FirstOrDefault(c => c.Rank == Rank.BlackJoker);
        Card redJoker = remaining.FirstOrDefault(c => c.Rank == Rank.RedJoker);

        if (blackJoker != null && redJoker != null)
        {
            List<Card> rocketCards = new List<Card> { blackJoker, redJoker };
            combos.Add(new CardCombo(ComboType.Rocket, Rank.RedJoker, rocketCards));
            remaining.Remove(blackJoker);
            remaining.Remove(redJoker);
        }
    }

    /// <summary>
    /// Extracts all bombs (four of a kind) from remaining cards.
    /// </summary>
    private static void ExtractBombs(List<Card> remaining, List<CardCombo> combos)
    {
        List<RankGroup> groups = GroupByRank(remaining);
        foreach (RankGroup group in groups)
        {
            if (group.Count == 4)
            {
                combos.Add(new CardCombo(ComboType.Bomb, group.Rank, group.Cards));
                foreach (Card card in group.Cards)
                    remaining.Remove(card);
            }
        }
    }

    /// <summary>
    /// Extracts the longest possible straights from remaining cards.
    /// Only uses ranks 3 through Ace, each card used at most once.
    /// </summary>
    private static void ExtractStraights(List<Card> remaining, List<CardCombo> combos)
    {
        while (true)
        {
            List<RankGroup> groups = GroupByRank(remaining);

            // Get all ranks that have at least 1 card and are valid for straights
            List<Rank> availableRanks = groups
                .Where(g => g.Count >= 1 && g.Rank >= Rank.Three && g.Rank <= Rank.Ace)
                .Select(g => g.Rank)
                .OrderBy(r => r)
                .ToList();

            // Find the longest consecutive sequence (minimum 5)
            List<Rank> bestRun = FindLongestRun(availableRanks, 5);
            if (bestRun == null)
                break;

            // Build the straight from actual cards
            List<Card> straightCards = new List<Card>();
            foreach (Rank rank in bestRun)
            {
                Card card = remaining.First(c => c.Rank == rank);
                straightCards.Add(card);
                remaining.Remove(card);
            }

            combos.Add(new CardCombo(ComboType.Straight, bestRun[bestRun.Count - 1], straightCards));
        }
    }

    /// <summary>
    /// Extracts consecutive pairs (3+ consecutive ranks, each with 2+ cards).
    /// </summary>
    private static void ExtractStraightPairs(List<Card> remaining, List<CardCombo> combos)
    {
        while (true)
        {
            List<RankGroup> groups = GroupByRank(remaining);

            List<Rank> availableRanks = groups
                .Where(g => g.Count >= 2 && g.Rank >= Rank.Three && g.Rank <= Rank.Ace)
                .Select(g => g.Rank)
                .OrderBy(r => r)
                .ToList();

            // Minimum 3 consecutive pairs
            List<Rank> bestRun = FindLongestRun(availableRanks, 3);
            if (bestRun == null)
                break;

            List<Card> pairCards = new List<Card>();
            foreach (Rank rank in bestRun)
            {
                List<Card> matching = remaining.Where(c => c.Rank == rank).Take(2).ToList();
                pairCards.AddRange(matching);
                foreach (Card card in matching)
                    remaining.Remove(card);
            }

            combos.Add(new CardCombo(ComboType.StraightPair, bestRun[bestRun.Count - 1], pairCards));
        }
    }

    /// <summary>
    /// Extracts planes (2+ consecutive triples), without kickers for simplicity.
    /// </summary>
    private static void ExtractPlanes(List<Card> remaining, List<CardCombo> combos)
    {
        while (true)
        {
            List<RankGroup> groups = GroupByRank(remaining);

            List<Rank> availableRanks = groups
                .Where(g => g.Count >= 3 && g.Rank >= Rank.Three && g.Rank <= Rank.Ace)
                .Select(g => g.Rank)
                .OrderBy(r => r)
                .ToList();

            // Minimum 2 consecutive triples
            List<Rank> bestRun = FindLongestRun(availableRanks, 2);
            if (bestRun == null)
                break;

            List<Card> planeCards = new List<Card>();
            foreach (Rank rank in bestRun)
            {
                List<Card> matching = remaining.Where(c => c.Rank == rank).Take(3).ToList();
                planeCards.AddRange(matching);
                foreach (Card card in matching)
                    remaining.Remove(card);
            }

            combos.Add(new CardCombo(ComboType.Plane, bestRun[bestRun.Count - 1], planeCards));
        }
    }

    /// <summary>
    /// Extracts triples. Tries to attach single kickers to reduce leftover singles.
    /// </summary>
    private static void ExtractTriples(List<Card> remaining, List<CardCombo> combos)
    {
        while (true)
        {
            List<RankGroup> groups = GroupByRank(remaining);
            RankGroup tripleGroup = groups.FirstOrDefault(g => g.Count >= 3);
            if (tripleGroup == null)
                break;

            List<Card> tripleCards = remaining.Where(c => c.Rank == tripleGroup.Rank).Take(3).ToList();
            foreach (Card card in tripleCards)
                remaining.Remove(card);

            // Try to find a single kicker (prefer smallest single)
            List<RankGroup> updatedGroups = GroupByRank(remaining);
            RankGroup kickerGroup = updatedGroups.FirstOrDefault(g => g.Count == 1);

            if (kickerGroup != null)
            {
                // Triple with single
                Card kicker = remaining.First(c => c.Rank == kickerGroup.Rank);
                tripleCards.Add(kicker);
                remaining.Remove(kicker);
                combos.Add(new CardCombo(ComboType.TripleWithSingle, tripleGroup.Rank, tripleCards));
            }
            else
            {
                // Pure triple
                combos.Add(new CardCombo(ComboType.Triple, tripleGroup.Rank, tripleCards));
            }
        }
    }

    /// <summary>
    /// Extracts all pairs from remaining cards.
    /// </summary>
    private static void ExtractPairs(List<Card> remaining, List<CardCombo> combos)
    {
        while (true)
        {
            List<RankGroup> groups = GroupByRank(remaining);
            RankGroup pairGroup = groups.FirstOrDefault(g => g.Count >= 2);
            if (pairGroup == null)
                break;

            List<Card> pairCards = remaining.Where(c => c.Rank == pairGroup.Rank).Take(2).ToList();
            foreach (Card card in pairCards)
                remaining.Remove(card);

            combos.Add(new CardCombo(ComboType.Pair, pairGroup.Rank, pairCards));
        }
    }

    /// <summary>
    /// Converts all remaining cards into single-card combos.
    /// </summary>
    private static void ExtractSingles(List<Card> remaining, List<CardCombo> combos)
    {
        foreach (Card card in remaining)
        {
            combos.Add(new CardCombo(ComboType.Single, card.Rank, new List<Card> { card }));
        }
        remaining.Clear();
    }

    // ==================== Utility ====================

    /// <summary>
    /// Finds the longest consecutive run in a sorted list of ranks.
    /// Returns null if no run meets the minimum length.
    /// </summary>
    private static List<Rank> FindLongestRun(List<Rank> sortedRanks, int minLength)
    {
        if (sortedRanks.Count < minLength)
            return null;

        List<Rank> best = null;
        List<Rank> current = new List<Rank> { sortedRanks[0] };

        for (int i = 1; i < sortedRanks.Count; i++)
        {
            if ((int)sortedRanks[i] - (int)sortedRanks[i - 1] == 1)
            {
                current.Add(sortedRanks[i]);
            }
            else
            {
                if (current.Count >= minLength && (best == null || current.Count > best.Count))
                    best = new List<Rank>(current);
                current = new List<Rank> { sortedRanks[i] };
            }
        }

        if (current.Count >= minLength && (best == null || current.Count > best.Count))
            best = current;

        return best;
    }
}
