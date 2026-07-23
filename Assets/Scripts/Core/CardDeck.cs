using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a full deck of 54 cards (52 standard + 2 jokers).
/// Handles deck creation, shuffling, and dealing for Dou Di Zhu.
/// </summary>
public class CardDeck
{
    private List<Card> cards = new List<Card>();

    /// <summary>
    /// Creates a new deck with 54 cards and shuffles it.
    /// </summary>
    public CardDeck()
    {
        InitDeck();
        Shuffle();
    }

    /// <summary>
    /// Initializes the deck with 52 suited cards + 2 jokers.
    /// </summary>
    private void InitDeck()
    {
        cards.Clear();

        // Add suited cards: 4 suits x 13 ranks (Three to Two)
        Suit[] suits = { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
        for (int rank = (int)Rank.Three; rank <= (int)Rank.Two; rank++)
        {
            foreach (Suit suit in suits)
            {
                cards.Add(new Card(suit, (Rank)rank));
            }
        }

        // Add two jokers
        cards.Add(new Card(Suit.None, Rank.BlackJoker));
        cards.Add(new Card(Suit.None, Rank.RedJoker));
    }

    /// <summary>
    /// Shuffles the deck using Fisher-Yates algorithm.
    /// </summary>
    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    /// <summary>
    /// Deals cards for Dou Di Zhu: 17 cards to each of 3 players, 3 cards as landlord extras.
    /// Returns a tuple of (player1Hand, player2Hand, player3Hand, landlordCards).
    /// </summary>
    public (List<Card>, List<Card>, List<Card>, List<Card>) Deal()
    {
        List<Card> player1 = cards.GetRange(0, 17);
        List<Card> player2 = cards.GetRange(17, 17);
        List<Card> player3 = cards.GetRange(34, 17);
        List<Card> landlordCards = cards.GetRange(51, 3);

        return (player1, player2, player3, landlordCards);
    }
}
