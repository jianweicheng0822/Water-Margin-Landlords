using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages turn-based card playing in Dou Di Zhu.
/// Handles turn rotation, card validation, pass logic, and win detection.
/// </summary>
public class TurnManager : MonoBehaviour
{
    // Index of the player whose turn it is
    private int currentPlayerIndex;

    // The last valid combo played on the table (null means free play)
    private CardCombo lastPlayedCombo;

    // The index of the player who played the last combo
    private int lastPlayedBy;

    // Number of consecutive passes
    private int consecutivePasses;

    // Whether the playing phase is active
    private bool isPlaying;

    // Reference to score manager for tracking bombs/rockets
    private ScoreManager scoreManager;

    /// <summary>
    /// Sets the score manager reference. Called during setup.
    /// </summary>
    public void SetScoreManager(ScoreManager manager)
    {
        scoreManager = manager;
    }

    /// <summary>
    /// Starts the playing phase. The landlord goes first with a free play.
    /// </summary>
    public void StartPlaying()
    {
        // Find the landlord to go first
        for (int i = 0; i < 3; i++)
        {
            if (GameManager.Instance.Players[i].IsLandlord)
            {
                currentPlayerIndex = i;
                break;
            }
        }

        lastPlayedCombo = null;
        lastPlayedBy = currentPlayerIndex;
        consecutivePasses = 0;
        isPlaying = true;

        Debug.Log($"Playing phase started. {GetCurrentPlayer().Name} (landlord) goes first.");
    }

    /// <summary>
    /// Attempts to play the selected cards from the current player's hand.
    /// Returns true if the play is valid and accepted.
    /// </summary>
    public bool PlayCards(List<Card> selectedCards)
    {
        if (!isPlaying)
            return false;

        Player player = GetCurrentPlayer();

        // Validate the selected cards form a legal combo
        CardCombo combo = CardRules.Evaluate(selectedCards);
        if (combo == null)
        {
            Debug.Log("Invalid combination.");
            return false;
        }

        // If there's a previous combo on the table, must beat it
        if (lastPlayedCombo != null && !combo.CanBeat(lastPlayedCombo))
        {
            Debug.Log($"Cannot beat {lastPlayedCombo}. Play a stronger combo or pass.");
            return false;
        }

        // Remove the played cards from the player's hand
        RemoveCardsFromHand(player, selectedCards);

        // Record the play for scoring (tracks bombs/rockets/play counts)
        if (scoreManager != null)
            scoreManager.RecordPlay(currentPlayerIndex, combo);

        // Update table state
        lastPlayedCombo = combo;
        lastPlayedBy = currentPlayerIndex;
        consecutivePasses = 0;

        Debug.Log($"{player.Name} plays: {combo} | Remaining cards: {player.Hand.Count}");

        // Check win condition
        if (player.Hand.Count == 0)
        {
            EndGame(player);
            return true;
        }

        // Advance to next player
        AdvanceTurn();
        return true;
    }

    /// <summary>
    /// Current player passes their turn.
    /// Cannot pass on a free play (when you start a new round).
    /// </summary>
    public bool Pass()
    {
        if (!isPlaying)
            return false;

        // Cannot pass on free play (you must play something)
        if (lastPlayedCombo == null)
        {
            Debug.Log($"{GetCurrentPlayer().Name} must play - cannot pass on free play.");
            return false;
        }

        Debug.Log($"{GetCurrentPlayer().Name} passes.");

        consecutivePasses++;

        // If 2 players pass in a row, the last player who played starts a new free round
        if (consecutivePasses >= 2)
        {
            lastPlayedCombo = null;
            currentPlayerIndex = lastPlayedBy;
            consecutivePasses = 0;
            Debug.Log($"Two passes. {GetCurrentPlayer().Name} starts a new round (free play).");
            return true;
        }

        AdvanceTurn();
        return true;
    }

    /// <summary>
    /// Removes the played cards from a player's hand.
    /// Matches by both suit and rank to remove the exact cards.
    /// </summary>
    private void RemoveCardsFromHand(Player player, List<Card> cardsToRemove)
    {
        foreach (Card card in cardsToRemove)
        {
            // Find and remove the matching card from hand
            Card match = player.Hand.FirstOrDefault(
                c => c.Rank == card.Rank && c.Suit == card.Suit
            );
            if (match != null)
            {
                player.Hand.Remove(match);
            }
        }
    }

    /// <summary>
    /// Advances the turn to the next player in order.
    /// </summary>
    private void AdvanceTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % 3;
        Debug.Log($"Now {GetCurrentPlayer().Name}'s turn.");
    }

    /// <summary>
    /// Ends the game and determines the winning side.
    /// </summary>
    private void EndGame(Player winner)
    {
        isPlaying = false;
        GameManager.Instance.CurrentPhase = GamePhase.GameOver;

        if (winner.IsLandlord)
        {
            Debug.Log($"Game over! {winner.Name} (landlord) wins!");
        }
        else
        {
            Debug.Log($"Game over! Farmers win! ({winner.Name} finished first)");
        }
    }

    /// <summary>
    /// Returns the player whose turn it is.
    /// </summary>
    public Player GetCurrentPlayer()
    {
        return GameManager.Instance.Players[currentPlayerIndex];
    }

    /// <summary>
    /// Returns the last combo played on the table (null if free play).
    /// </summary>
    public CardCombo GetLastPlayedCombo()
    {
        return lastPlayedCombo;
    }

    /// <summary>
    /// Returns true if the playing phase is active.
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }
}
