using UnityEngine;

/// <summary>
/// Manages the bidding (叫地主/抢地主) phase of a Dou Di Zhu game.
/// Randomly selects a starting player, then each player decides to bid or pass.
/// If no one bids, the cards are re-dealt.
/// </summary>
public class BidManager : MonoBehaviour
{
    // The index of the player who gets to bid first
    private int startPlayerIndex;

    // The index of the player currently bidding
    private int currentBidder;

    // How many players have passed in a row
    private int passCount;

    // Whether bidding is currently active
    private bool isBidding;

    /// <summary>
    /// Starts the bidding phase. Randomly picks who goes first.
    /// </summary>
    public void StartBidding()
    {
        startPlayerIndex = Random.Range(0, 3);
        currentBidder = startPlayerIndex;
        passCount = 0;
        isBidding = true;

        Debug.Log($"Bidding started. {GameManager.Instance.Players[currentBidder].Name} bids first.");
    }

    /// <summary>
    /// Called when the current bidder decides to bid (call landlord).
    /// The bidder becomes the landlord immediately.
    /// </summary>
    public void Bid()
    {
        if (!isBidding)
            return;

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} calls landlord!");

        isBidding = false;
        GameManager.Instance.AssignLandlord(currentBidder);
    }

    /// <summary>
    /// Called when the current bidder decides to pass.
    /// If all 3 players pass, the game re-deals.
    /// </summary>
    public void Pass()
    {
        if (!isBidding)
            return;

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} passes.");

        passCount++;

        // All 3 players passed, re-deal
        if (passCount >= 3)
        {
            Debug.Log("All players passed. Re-dealing...");
            isBidding = false;
            GameManager.Instance.StartGame();
            return;
        }

        // Move to next player
        currentBidder = (currentBidder + 1) % 3;
        Debug.Log($"Now {GameManager.Instance.Players[currentBidder].Name}'s turn to bid.");
    }

    /// <summary>
    /// Returns the index of the player who is currently bidding.
    /// </summary>
    public int GetCurrentBidder()
    {
        return currentBidder;
    }

    /// <summary>
    /// Returns true if bidding is still in progress.
    /// </summary>
    public bool IsBidding()
    {
        return isBidding;
    }
}
