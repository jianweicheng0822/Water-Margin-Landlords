using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the current phase of a Dou Di Zhu game.
/// </summary>
public enum GamePhase
{
    Idle,       // Waiting to start
    Dealing,    // Cards are being dealt
    Bidding,    // Players are bidding to become landlord
    Playing,    // Main gameplay phase, players take turns
    GameOver    // Game has ended, showing results
}

/// <summary>
/// Represents a player in the game (human or AI).
/// </summary>
public class Player
{
    public int Index { get; private set; }         // Player seat index (0, 1, 2)
    public string Name { get; private set; }
    public List<Card> Hand { get; private set; }   // Cards in hand
    public bool IsLandlord { get; set; }           // Whether this player is the landlord

    public Player(int index, string name)
    {
        Index = index;
        Name = name;
        Hand = new List<Card>();
        IsLandlord = false;
    }

    /// <summary>
    /// Sorts the hand by rank (descending) for display purposes.
    /// </summary>
    public void SortHand()
    {
        Hand.Sort((a, b) => b.Rank.CompareTo(a.Rank));
    }
}

/// <summary>
/// Central game manager that controls the overall flow of a Dou Di Zhu game.
/// Attach this to a GameObject in the scene to run the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GamePhase CurrentPhase { get; set; } = GamePhase.Idle;

    // Three players: index 0 is the human player, 1 and 2 are AI
    public Player[] Players { get; private set; } = new Player[3];

    // The 3 landlord bonus cards (revealed after bidding)
    public List<Card> LandlordCards { get; private set; } = new List<Card>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Starts a new game: creates players, deals cards.
    /// </summary>
    public void StartGame()
    {
        // Initialize players
        Players[0] = new Player(0, "\u4f60");        // 你
        Players[1] = new Player(1, "\u6797\u51b2");    // 林冲
        Players[2] = new Player(2, "\u9c81\u667a\u6df1");  // 鲁智深

        // Deal cards
        DealCards();

        // Move to bidding phase
        CurrentPhase = GamePhase.Bidding;
        Debug.Log("Game started. Entering bidding phase.");

        // Log each player's hand for debugging
        for (int i = 0; i < 3; i++)
        {
            string handStr = string.Join(", ", Players[i].Hand.Select(c => c.ToString()));
            Debug.Log($"{Players[i].Name}'s hand ({Players[i].Hand.Count} cards): {handStr}");
        }

        string landlordStr = string.Join(", ", LandlordCards.Select(c => c.ToString()));
        Debug.Log($"Landlord cards: {landlordStr}");
    }

    /// <summary>
    /// Creates a deck, shuffles it, and deals 17 cards to each player + 3 landlord cards.
    /// </summary>
    private void DealCards()
    {
        CurrentPhase = GamePhase.Dealing;

        CardDeck deck = new CardDeck();
        var (hand1, hand2, hand3, landlordCards) = deck.Deal();

        Players[0].Hand.AddRange(hand1);
        Players[1].Hand.AddRange(hand2);
        Players[2].Hand.AddRange(hand3);
        LandlordCards = new List<Card>(landlordCards);

        // Sort all hands for readability
        for (int i = 0; i < 3; i++)
        {
            Players[i].SortHand();
        }
    }

    /// <summary>
    /// Assigns the landlord role: gives the 3 bonus cards to the winning bidder.
    /// Called after bidding is resolved.
    /// </summary>
    public void AssignLandlord(int playerIndex)
    {
        Player landlord = Players[playerIndex];
        landlord.IsLandlord = true;

        // Add the 3 landlord cards to the landlord's hand
        landlord.Hand.AddRange(LandlordCards);
        landlord.SortHand();

        Debug.Log($"{landlord.Name} is the landlord! Hand now has {landlord.Hand.Count} cards.");

        // Move to playing phase
        CurrentPhase = GamePhase.Playing;
    }
}
