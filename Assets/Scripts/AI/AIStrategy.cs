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

    // ==================== Play Decision ====================

    /// <summary>
    /// Chooses which combo to lead with during a free play (no previous combo to beat).
    /// Strategy: play the smallest non-bomb combo to preserve strong cards.
    /// Returns null only if hand is empty (should not happen in normal play).
    /// </summary>
    public static CardCombo ChooseLeadPlay(List<Card> hand)
    {
        List<CardCombo> combos = DecomposeHand(hand);
        if (combos.Count == 0)
            return null;

        // If only one combo left, play it (even if it's a bomb)
        if (combos.Count == 1)
            return combos[0];

        // Sort combos: non-bombs first, then by main rank ascending
        combos.Sort((a, b) =>
        {
            // Bombs and rockets go last
            int aWeight = (a.Type == ComboType.Bomb || a.Type == ComboType.Rocket) ? 1 : 0;
            int bWeight = (b.Type == ComboType.Bomb || b.Type == ComboType.Rocket) ? 1 : 0;
            if (aWeight != bWeight)
                return aWeight.CompareTo(bWeight);

            // Among same priority, prefer smaller rank
            return a.MainRank.CompareTo(b.MainRank);
        });

        // Play the smallest non-bomb combo
        return combos[0];
    }

    /// <summary>
    /// Chooses which combo to play to beat the last played combo.
    /// Strategy: find all possible plays that beat it, pick the smallest one.
    /// Returns null if the AI decides to pass.
    /// </summary>
    public static CardCombo ChooseFollowPlay(List<Card> hand, CardCombo lastPlayed)
    {
        List<CardCombo> candidates = FindBeatingCombos(hand, lastPlayed);

        if (candidates.Count == 0)
            return null; // Must pass

        // Sort by main rank ascending to pick the smallest winning play
        candidates.Sort((a, b) =>
        {
            // Prefer non-bombs over bombs (save bombs for later)
            int aWeight = (a.Type == ComboType.Bomb || a.Type == ComboType.Rocket) ? 1 : 0;
            int bWeight = (b.Type == ComboType.Bomb || b.Type == ComboType.Rocket) ? 1 : 0;
            if (aWeight != bWeight)
                return aWeight.CompareTo(bWeight);

            return a.MainRank.CompareTo(b.MainRank);
        });

        return candidates[0];
    }

    /// <summary>
    /// Finds all possible combos from the hand that can beat the last played combo.
    /// Searches for same-type combos with higher rank, plus any bombs/rockets.
    /// </summary>
    private static List<CardCombo> FindBeatingCombos(List<Card> hand, CardCombo lastPlayed)
    {
        List<CardCombo> results = new List<CardCombo>();
        Dictionary<Rank, List<Card>> rankGroups = new Dictionary<Rank, List<Card>>();

        foreach (Card card in hand)
        {
            if (!rankGroups.ContainsKey(card.Rank))
                rankGroups[card.Rank] = new List<Card>();
            rankGroups[card.Rank].Add(card);
        }

        // Always check for rockets
        if (rankGroups.ContainsKey(Rank.BlackJoker) && rankGroups.ContainsKey(Rank.RedJoker))
        {
            List<Card> rocketCards = new List<Card>
            {
                rankGroups[Rank.BlackJoker][0],
                rankGroups[Rank.RedJoker][0]
            };
            results.Add(new CardCombo(ComboType.Rocket, Rank.RedJoker, rocketCards));
        }

        // Always check for bombs (unless last played is a rocket)
        if (lastPlayed.Type != ComboType.Rocket)
        {
            foreach (var kv in rankGroups)
            {
                if (kv.Value.Count == 4)
                {
                    CardCombo bomb = new CardCombo(ComboType.Bomb, kv.Key, kv.Value);
                    // Only add if it actually beats the last played
                    if (bomb.CanBeat(lastPlayed))
                        results.Add(bomb);
                }
            }
        }

        // Find same-type combos that beat the last played
        switch (lastPlayed.Type)
        {
            case ComboType.Single:
                FindBeatingSingles(hand, lastPlayed, results);
                break;
            case ComboType.Pair:
                FindBeatingPairs(rankGroups, lastPlayed, results);
                break;
            case ComboType.Triple:
            case ComboType.TripleWithSingle:
            case ComboType.TripleWithPair:
                FindBeatingTriples(hand, rankGroups, lastPlayed, results);
                break;
            case ComboType.Straight:
                FindBeatingStraights(hand, lastPlayed, results);
                break;
            case ComboType.StraightPair:
                FindBeatingStraightPairs(hand, rankGroups, lastPlayed, results);
                break;
            case ComboType.Plane:
            case ComboType.PlaneWithSingles:
            case ComboType.PlaneWithPairs:
                FindBeatingPlanes(hand, rankGroups, lastPlayed, results);
                break;
            case ComboType.FourWithTwo:
            case ComboType.FourWithTwoPairs:
                FindBeatingFourWithTwo(hand, rankGroups, lastPlayed, results);
                break;
            default:
                break;
        }

        return results;
    }

    /// <summary>
    /// Finds all single cards that beat the last played single.
    /// </summary>
    private static void FindBeatingSingles(List<Card> hand, CardCombo lastPlayed, List<CardCombo> results)
    {
        // Use a set to avoid duplicates (only need one card per rank)
        HashSet<Rank> addedRanks = new HashSet<Rank>();
        foreach (Card card in hand)
        {
            if (card.Rank > lastPlayed.MainRank && !addedRanks.Contains(card.Rank))
            {
                results.Add(new CardCombo(ComboType.Single, card.Rank, new List<Card> { card }));
                addedRanks.Add(card.Rank);
            }
        }
    }

    /// <summary>
    /// Finds all pairs that beat the last played pair.
    /// </summary>
    private static void FindBeatingPairs(Dictionary<Rank, List<Card>> rankGroups, CardCombo lastPlayed, List<CardCombo> results)
    {
        foreach (var kv in rankGroups)
        {
            if (kv.Value.Count >= 2 && kv.Key > lastPlayed.MainRank && !kv.Value[0].IsJoker)
            {
                List<Card> pairCards = kv.Value.Take(2).ToList();
                results.Add(new CardCombo(ComboType.Pair, kv.Key, pairCards));
            }
        }
    }

    /// <summary>
    /// Finds all triple combos (with matching kicker type) that beat the last played triple.
    /// </summary>
    private static void FindBeatingTriples(List<Card> hand, Dictionary<Rank, List<Card>> rankGroups, CardCombo lastPlayed, List<CardCombo> results)
    {
        foreach (var kv in rankGroups)
        {
            if (kv.Value.Count >= 3 && kv.Key > lastPlayed.MainRank)
            {
                List<Card> tripleCards = kv.Value.Take(3).ToList();

                if (lastPlayed.Type == ComboType.Triple)
                {
                    results.Add(new CardCombo(ComboType.Triple, kv.Key, tripleCards));
                }
                else if (lastPlayed.Type == ComboType.TripleWithSingle)
                {
                    // Find any single kicker
                    Card kicker = hand.FirstOrDefault(c => c.Rank != kv.Key);
                    if (kicker != null)
                    {
                        List<Card> combo = new List<Card>(tripleCards) { kicker };
                        results.Add(new CardCombo(ComboType.TripleWithSingle, kv.Key, combo));
                    }
                }
                else if (lastPlayed.Type == ComboType.TripleWithPair)
                {
                    // Find a pair kicker
                    foreach (var kv2 in rankGroups)
                    {
                        if (kv2.Key != kv.Key && kv2.Value.Count >= 2 && !kv2.Value[0].IsJoker)
                        {
                            List<Card> combo = new List<Card>(tripleCards);
                            combo.AddRange(kv2.Value.Take(2));
                            results.Add(new CardCombo(ComboType.TripleWithPair, kv.Key, combo));
                            break; // One pair kicker is enough
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds all straights of the same length that beat the last played straight.
    /// </summary>
    private static void FindBeatingStraights(List<Card> hand, CardCombo lastPlayed, List<CardCombo> results)
    {
        int length = lastPlayed.Length;
        Dictionary<Rank, List<Card>> rankGroups = new Dictionary<Rank, List<Card>>();
        foreach (Card card in hand)
        {
            if (!rankGroups.ContainsKey(card.Rank))
                rankGroups[card.Rank] = new List<Card>();
            rankGroups[card.Rank].Add(card);
        }

        // Try all possible starting ranks for a straight of the same length
        for (int startRank = (int)lastPlayed.MainRank - length + 2; startRank <= (int)Rank.Ace - length + 1; startRank++)
        {
            Rank endRank = (Rank)(startRank + length - 1);

            // End rank must be higher than last played
            if (endRank <= lastPlayed.MainRank)
                continue;

            // Check if all ranks in range are available
            bool valid = true;
            List<Card> straightCards = new List<Card>();
            for (int r = startRank; r <= (int)endRank; r++)
            {
                Rank rank = (Rank)r;
                if (!rankGroups.ContainsKey(rank) || rankGroups[rank].Count == 0)
                {
                    valid = false;
                    break;
                }
                straightCards.Add(rankGroups[rank][0]);
            }

            if (valid)
                results.Add(new CardCombo(ComboType.Straight, endRank, straightCards));
        }
    }

    /// <summary>
    /// Finds all straight pairs of the same length that beat the last played straight pair.
    /// </summary>
    private static void FindBeatingStraightPairs(List<Card> hand, Dictionary<Rank, List<Card>> rankGroups, CardCombo lastPlayed, List<CardCombo> results)
    {
        int pairCount = lastPlayed.Length / 2;

        for (int startRank = (int)lastPlayed.MainRank - pairCount + 2; startRank <= (int)Rank.Ace - pairCount + 1; startRank++)
        {
            Rank endRank = (Rank)(startRank + pairCount - 1);

            if (endRank <= lastPlayed.MainRank)
                continue;

            bool valid = true;
            List<Card> pairCards = new List<Card>();
            for (int r = startRank; r <= (int)endRank; r++)
            {
                Rank rank = (Rank)r;
                if (!rankGroups.ContainsKey(rank) || rankGroups[rank].Count < 2)
                {
                    valid = false;
                    break;
                }
                pairCards.AddRange(rankGroups[rank].Take(2));
            }

            if (valid)
                results.Add(new CardCombo(ComboType.StraightPair, endRank, pairCards));
        }
    }

    /// <summary>
    /// Finds all plane combos (with matching kicker type) that beat the last played plane.
    /// Searches for consecutive triples with higher rank, then attaches appropriate kickers.
    /// </summary>
    private static void FindBeatingPlanes(List<Card> hand, Dictionary<Rank, List<Card>> rankGroups, CardCombo lastPlayed, List<CardCombo> results)
    {
        // Determine how many consecutive triples the last played plane has
        int tripleCount;
        if (lastPlayed.Type == ComboType.Plane)
            tripleCount = lastPlayed.Length / 3;
        else if (lastPlayed.Type == ComboType.PlaneWithSingles)
            tripleCount = lastPlayed.Length / 4; // 3 per triple + 1 kicker each
        else // PlaneWithPairs
            tripleCount = lastPlayed.Length / 5; // 3 per triple + 2 kicker each

        // Find all ranks with 3+ cards that could form consecutive triples
        List<Rank> tripleRanks = rankGroups
            .Where(kv => kv.Value.Count >= 3 && kv.Key >= Rank.Three && kv.Key <= Rank.Ace)
            .Select(kv => kv.Key)
            .OrderBy(r => r)
            .ToList();

        // Try all consecutive sequences of the required length
        for (int i = 0; i <= tripleRanks.Count - tripleCount; i++)
        {
            // Check if this subsequence is consecutive
            List<Rank> sequence = new List<Rank>();
            bool consecutive = true;
            for (int j = 0; j < tripleCount; j++)
            {
                if (j > 0 && (int)tripleRanks[i + j] - (int)tripleRanks[i + j - 1] != 1)
                {
                    consecutive = false;
                    break;
                }
                sequence.Add(tripleRanks[i + j]);
            }

            if (!consecutive)
                continue;

            Rank highestRank = sequence[sequence.Count - 1];

            // Must beat the last played (higher main rank)
            if (highestRank <= lastPlayed.MainRank)
                continue;

            // Build the triple cards
            List<Card> planeCards = new List<Card>();
            HashSet<Rank> usedRanks = new HashSet<Rank>();
            foreach (Rank rank in sequence)
            {
                planeCards.AddRange(rankGroups[rank].Take(3));
                usedRanks.Add(rank);
            }

            if (lastPlayed.Type == ComboType.Plane)
            {
                // Pure plane, no kickers needed
                results.Add(new CardCombo(ComboType.Plane, highestRank, planeCards));
            }
            else if (lastPlayed.Type == ComboType.PlaneWithSingles)
            {
                // Need one single kicker per triple from non-plane ranks
                List<Card> kickers = new List<Card>();
                foreach (Card card in hand)
                {
                    if (!usedRanks.Contains(card.Rank) && kickers.Count < tripleCount)
                    {
                        // Avoid using a card already picked as kicker
                        if (!kickers.Exists(k => k.Rank == card.Rank && k.Suit == card.Suit))
                            kickers.Add(card);
                    }
                }

                if (kickers.Count == tripleCount)
                {
                    List<Card> combo = new List<Card>(planeCards);
                    combo.AddRange(kickers);
                    results.Add(new CardCombo(ComboType.PlaneWithSingles, highestRank, combo));
                }
            }
            else // PlaneWithPairs
            {
                // Need one pair kicker per triple from non-plane ranks
                List<Card> kickers = new List<Card>();
                int pairsFound = 0;
                foreach (var kv in rankGroups)
                {
                    if (!usedRanks.Contains(kv.Key) && kv.Value.Count >= 2 && !kv.Value[0].IsJoker && pairsFound < tripleCount)
                    {
                        kickers.AddRange(kv.Value.Take(2));
                        pairsFound++;
                    }
                }

                if (pairsFound == tripleCount)
                {
                    List<Card> combo = new List<Card>(planeCards);
                    combo.AddRange(kickers);
                    results.Add(new CardCombo(ComboType.PlaneWithPairs, highestRank, combo));
                }
            }
        }
    }

    /// <summary>
    /// Finds all four-with-two combos that beat the last played four-with-two.
    /// Matches the kicker type (singles or pairs) of the last played combo.
    /// </summary>
    private static void FindBeatingFourWithTwo(List<Card> hand, Dictionary<Rank, List<Card>> rankGroups, CardCombo lastPlayed, List<CardCombo> results)
    {
        foreach (var kv in rankGroups)
        {
            if (kv.Value.Count == 4 && kv.Key > lastPlayed.MainRank)
            {
                List<Card> quadCards = new List<Card>(kv.Value);

                if (lastPlayed.Type == ComboType.FourWithTwo)
                {
                    // Need 2 single kickers from other ranks
                    List<Card> kickers = new List<Card>();
                    foreach (Card card in hand)
                    {
                        if (card.Rank != kv.Key && kickers.Count < 2)
                        {
                            if (!kickers.Exists(k => k.Rank == card.Rank && k.Suit == card.Suit))
                                kickers.Add(card);
                        }
                    }

                    if (kickers.Count == 2)
                    {
                        List<Card> combo = new List<Card>(quadCards);
                        combo.AddRange(kickers);
                        results.Add(new CardCombo(ComboType.FourWithTwo, kv.Key, combo));
                    }
                }
                else // FourWithTwoPairs
                {
                    // Need 2 pair kickers from other ranks
                    List<Card> kickers = new List<Card>();
                    int pairsFound = 0;
                    foreach (var kv2 in rankGroups)
                    {
                        if (kv2.Key != kv.Key && kv2.Value.Count >= 2 && !kv2.Value[0].IsJoker && pairsFound < 2)
                        {
                            kickers.AddRange(kv2.Value.Take(2));
                            pairsFound++;
                        }
                    }

                    if (pairsFound == 2)
                    {
                        List<Card> combo = new List<Card>(quadCards);
                        combo.AddRange(kickers);
                        results.Add(new CardCombo(ComboType.FourWithTwoPairs, kv.Key, combo));
                    }
                }
            }
        }
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
