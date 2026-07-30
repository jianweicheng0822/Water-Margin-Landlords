using UnityEngine;

/// <summary>
/// Manages the "grab landlord" (抢地主) bidding phase of a Dou Di Zhu game.
/// Two-phase system:
///   Phase 1 (Calling): Players take turns choosing "叫地主" or "不叫".
///   Phase 2 (Grabbing): Once someone calls, the other two each get one chance to "抢地主" or "不抢".
/// Each grab doubles the multiplier: ×1 (base) → ×2 (one grab) → ×4 (two grabs).
/// The last person to grab becomes landlord; if nobody grabs, the caller stays.
/// If all three pass without anyone calling, the game re-deals.
/// </summary>
public class BidManager : MonoBehaviour
{
    /// <summary>
    /// The two phases of the bidding process.
    /// </summary>
    public enum BidPhase
    {
        Calling,   // Players decide whether to call landlord
        Grabbing   // After someone calls, others decide whether to grab
    }

    // The index of the player currently making a decision
    private int currentBidder;

    // Current phase of the bidding process
    private BidPhase currentPhase;

    // The index of the player who called landlord (-1 = no one yet)
    private int callerIndex;

    // The index of the player who will become landlord (updated as grabs happen)
    private int landlordIndex;

    // How many players have had a turn in the current phase
    private int turnsTaken;

    // How many grab turns have been taken in phase 2
    private int grabTurnsTaken;

    // Current multiplier: 1 (base), 2 (one grab), 4 (two grabs)
    private int grabMultiplier;

    // Whether bidding is currently active
    private bool isBidding;

    /// <summary>
    /// The final multiplier from the grab phase (1, 2, or 4).
    /// Used as base multiplier for scoring.
    /// </summary>
    public int Multiplier => grabMultiplier;

    /// <summary>
    /// Returns the current bidding phase (Calling or Grabbing).
    /// </summary>
    public BidPhase CurrentPhase => currentPhase;

    /// <summary>
    /// Starts the bidding phase. Randomly picks who goes first.
    /// </summary>
    public void StartBidding()
    {
        currentBidder = Random.Range(0, 3);
        currentPhase = BidPhase.Calling;
        callerIndex = -1;
        landlordIndex = -1;
        turnsTaken = 0;
        grabTurnsTaken = 0;
        grabMultiplier = 1;
        isBidding = true;

        Debug.Log($"Bidding started (grab landlord mode). {GameManager.Instance.Players[currentBidder].Name} decides first.");
    }

    /// <summary>
    /// Called when the current player calls landlord (叫地主) during the Calling phase.
    /// Transitions to Grabbing phase.
    /// </summary>
    public void CallLandlord()
    {
        if (!isBidding || currentPhase != BidPhase.Calling)
            return;

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} calls landlord!");

        callerIndex = currentBidder;
        landlordIndex = currentBidder;
        turnsTaken = 0;
        grabTurnsTaken = 0;
        currentPhase = BidPhase.Grabbing;

        // Move to next player for grabbing
        AdvanceBidder();
    }

    /// <summary>
    /// Called when the current player grabs landlord (抢地主) during the Grabbing phase.
    /// Doubles the multiplier and updates who becomes landlord.
    /// </summary>
    public void GrabLandlord()
    {
        if (!isBidding || currentPhase != BidPhase.Grabbing)
            return;

        Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} grabs landlord! Multiplier doubles.");

        // Each grab doubles the multiplier
        grabMultiplier *= 2;
        landlordIndex = currentBidder;
        grabTurnsTaken++;

        // Check if both other players have had their grab turn
        if (grabTurnsTaken >= 2)
        {
            FinishBidding();
            return;
        }

        // Move to next player for their grab chance
        AdvanceBidder();
    }

    /// <summary>
    /// Called when the current player passes (不叫 in Calling phase, 不抢 in Grabbing phase).
    /// </summary>
    public void Pass()
    {
        if (!isBidding)
            return;

        if (currentPhase == BidPhase.Calling)
        {
            Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} passes (不叫).");
            turnsTaken++;

            // All 3 players passed without anyone calling — re-deal
            if (turnsTaken >= 3)
            {
                Debug.Log("All players passed. Re-dealing...");
                isBidding = false;
                GameManager.Instance.StartGame();
                return;
            }

            // Move to next player
            AdvanceBidder();
        }
        else // Grabbing phase
        {
            Debug.Log($"{GameManager.Instance.Players[currentBidder].Name} passes (不抢).");
            grabTurnsTaken++;

            // Check if both other players have had their grab turn
            if (grabTurnsTaken >= 2)
            {
                FinishBidding();
                return;
            }

            // Move to next player for their grab chance
            AdvanceBidder();
        }
    }

    /// <summary>
    /// Advances to the next bidder in seat order.
    /// In Grabbing phase, skips the caller (they already called).
    /// </summary>
    private void AdvanceBidder()
    {
        currentBidder = (currentBidder + 1) % 3;

        // In grabbing phase, skip the caller — they already committed
        if (currentPhase == BidPhase.Grabbing && currentBidder == callerIndex)
        {
            currentBidder = (currentBidder + 1) % 3;
        }

        string phaseStr = currentPhase == BidPhase.Calling ? "call" : "grab";
        Debug.Log($"Now {GameManager.Instance.Players[currentBidder].Name}'s turn to {phaseStr}.");
    }

    /// <summary>
    /// Ends bidding and assigns the landlord role.
    /// </summary>
    private void FinishBidding()
    {
        isBidding = false;
        Debug.Log($"Bidding complete! {GameManager.Instance.Players[landlordIndex].Name} is landlord. Multiplier: x{grabMultiplier}.");
        GameManager.Instance.AssignLandlord(landlordIndex);
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
