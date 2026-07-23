using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enumerates all valid card combination types in Dou Di Zhu.
/// </summary>
public enum ComboType
{
    Invalid,          // Not a valid combination
    Single,           // Single card (e.g. 3)
    Pair,             // Two cards of the same rank (e.g. 33)
    Triple,           // Three cards of the same rank (e.g. 333)
    TripleWithSingle, // Three + one kicker (e.g. 333+4)
    TripleWithPair,   // Three + a pair as kicker (e.g. 333+44)
    Straight,         // Five or more consecutive singles (e.g. 34567)
    StraightPair,     // Three or more consecutive pairs (e.g. 334455)
    Plane,            // Two or more consecutive triples (e.g. 333444)
    PlaneWithSingles, // Consecutive triples + same number of single kickers (e.g. 333444+5+6)
    PlaneWithPairs,   // Consecutive triples + same number of pair kickers (e.g. 333444+55+66)
    FourWithTwo,      // Four of a kind + two single kickers (e.g. 3333+4+5)
    FourWithTwoPairs, // Four of a kind + two pair kickers (e.g. 3333+44+55)
    Bomb,             // Four cards of the same rank (e.g. 3333), beats everything except Rocket
    Rocket            // Black Joker + Red Joker, beats everything
}

/// <summary>
/// Represents a validated card combination with its type and key rank for comparison.
/// </summary>
public class CardCombo
{
    /// <summary>
    /// The type of this combination.
    /// </summary>
    public ComboType Type { get; private set; }

    /// <summary>
    /// The main rank used for comparison.
    /// For single/pair/triple/bomb: the rank of those cards.
    /// For straights: the highest rank in the sequence.
    /// For plane: the highest rank of the consecutive triples.
    /// </summary>
    public Rank MainRank { get; private set; }

    /// <summary>
    /// The number of cards in this combination.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// The actual cards that make up this combination.
    /// </summary>
    public List<Card> Cards { get; private set; }

    public CardCombo(ComboType type, Rank mainRank, List<Card> cards)
    {
        Type = type;
        MainRank = mainRank;
        Length = cards.Count;
        Cards = new List<Card>(cards);
    }

    /// <summary>
    /// Returns true if this combo can beat the other combo.
    /// Rocket beats everything. Bomb beats non-bomb/non-rocket.
    /// Otherwise, same type and length required, higher main rank wins.
    /// </summary>
    public bool CanBeat(CardCombo other)
    {
        // Rocket beats everything
        if (Type == ComboType.Rocket)
            return true;

        // Bomb beats anything except rocket
        if (Type == ComboType.Bomb && other.Type != ComboType.Bomb && other.Type != ComboType.Rocket)
            return true;

        // Bomb vs bomb: compare rank
        if (Type == ComboType.Bomb && other.Type == ComboType.Bomb)
            return MainRank > other.MainRank;

        // Same type and same length: compare main rank
        if (Type == other.Type && Length == other.Length)
            return MainRank > other.MainRank;

        // Different types (non-bomb): cannot beat
        return false;
    }

    public override string ToString()
    {
        return $"{Type} (MainRank: {MainRank}, Cards: {Length})";
    }
}
