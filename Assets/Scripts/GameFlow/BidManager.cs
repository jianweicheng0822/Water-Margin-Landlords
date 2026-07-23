using UnityEngine;

/// <summary>
/// Manages the score-based bidding phase of a Dou Di Zhu game.
/// Players can bid 1, 2, or 3 points. Each subsequent player must bid higher or pass.
/// The highest bidder becomes the landlord. The bid score determines the base multiplier.
/// </summary>
public class BidManager : MonoBehaviour
{
    // The index of the player currently bidding
    private int currentBidder;

    // The current highest bid (0 = no one has bid yet)
    private int highestBid;

    // The index of the player with the highest bid (-1 = no one)
    private int highestBidder;

    // How many players have had a turn to bid
    private int turnsTaken;

    // Whether bidding is currently active
    private bool isBidding;

    /// <summary>
    /// The final bid score (1, 2, or 3). Used as base multiplier for scoring.
    /// </summary>
    public int FinalBidScore { get; private set; }

    /// <summary>
    /// Starts the bidding phase. Randomly picks who goes first.
    /// </summary>
    public void StartBidding()
    {
        currentBidder = Random.Range(0, 3);
        highestBid = 0;
        highestBidder = -1;
        turnsTaken = 0;
        isBidding = true;
        FinalBidScore = 0;

        Debug.Log($"Bidding started. {GameManager.Instance.Players[currentBidder].Name} bids first.");
    }

    /// <summary>
    /// Called when the current bidder places a score bid (1, 2, or 3).
    /// Must be higher than the current highest bid.
    /// If someone bids 3, they become landlord immediately.
    /// </summary>
    public void BidScore(int score)
    {
        if (!isBidding)
            return;

        if (score <= highestBid || score < 1 || score > 3)
        {
            Debug.Log($"Invalid bid: {score}. Must be higher than {highestBid}.");
            return;
        }

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} bids {score} points!");

        highestBid = score;
        highestBidder = currentBidder;
        turnsTaken++;

        // Bid 3 ends bidding immediately (maximum possible)
        if (score == 3)
        {
            FinishBidding();
            return;
        }

        // Move to next player
        AdvanceBidder();
    }

    /// <summary>
    /// Called when the current bidder decides to pass.
    /// </summary>
    public void Pass()
    {
        if (!isBidding)
            return;

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} passes.");

        turnsTaken++;

        // All 3 players have had a turn
        if (turnsTaken >= 3)
        {
            if (highestBidder == -1)
            {
                // No one bid at all, need to re-deal
                Debug.Log("All players passed. Re-dealing...");
                isBidding = false;
                GameManager.Instance.StartGame();
                return;
            }
            else
            {
                // Someone bid, they become landlord
                FinishBidding();
                return;
            }
        }

        // Move to next player
        AdvanceBidder();
    }

    /// <summary>
    /// Advances to the next bidder in seat order.
    /// </summary>
    private void AdvanceBidder()
    {
        currentBidder = (currentBidder + 1) % 3;
        Debug.Log($"Now {GameManager.Instance.Players[currentBidder].Name}'s turn to bid. Current highest: {highestBid}");
    }

    /// <summary>
    /// Ends bidding and assigns the landlord role to the highest bidder.
    /// </summary>
    private void FinishBidding()
    {
        isBidding = false;
        FinalBidScore = highestBid;
        Debug.Log($"Bidding complete! {GameManager.Instance.Players[highestBidder].Name} wins with {highestBid} points.");
        GameManager.Instance.AssignLandlord(highestBidder);
    }

    /// <summary>
    /// Returns the index of the player who is currently bidding.
    /// </summary>
    public int GetCurrentBidder()
    {
        return currentBidder;
    }

    /// <summary>
    /// Returns the current highest bid score (0 if no one has bid yet).
    /// </summary>
    public int GetHighestBid()
    {
        return highestBid;
    }

    /// <summary>
    /// Returns true if bidding is still in progress.
    /// </summary>
    public bool IsBidding()
    {
        return isBidding;
    }
}
