using UnityEngine;

/// <summary>
/// Represents the four suits in a standard deck of cards.
/// None is used for jokers which have no suit.
/// </summary>
public enum Suit
{
    None,    // For jokers
    Spade,
    Heart,
    Diamond,
    Club
}

/// <summary>
/// Represents card ranks in Dou Di Zhu order.
/// In Dou Di Zhu, the rank order from lowest to highest is:
/// 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K, A, 2, Black Joker, Red Joker.
/// The enum values reflect this ordering for easy comparison.
/// </summary>
public enum Rank
{
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14,
    Two = 15,
    BlackJoker = 16,
    RedJoker = 17
}

/// <summary>
/// Represents a single playing card with a suit and rank.
/// </summary>
public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Returns true if this card is a joker (Black or Red).
    /// </summary>
    public bool IsJoker => Rank == Rank.BlackJoker || Rank == Rank.RedJoker;

    /// <summary>
    /// Returns a readable string for debugging, e.g. "Spade_Ace" or "BlackJoker".
    /// </summary>
    public override string ToString()
    {
        if (IsJoker)
            return Rank.ToString();
        return $"{Suit}_{Rank}";
    }
}
