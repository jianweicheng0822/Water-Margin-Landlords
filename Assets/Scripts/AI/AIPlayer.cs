using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls an AI player in Dou Di Zhu.
/// Connects AIStrategy decisions to GameManager and TurnManager.
/// Attach this to the same GameObject as GameManager.
/// </summary>
public class AIPlayer : MonoBehaviour
{
    private TurnManager turnManager;
    private BidManager bidManager;

    /// <summary>
    /// Initializes references to managers. Called by GameSetup.
    /// </summary>
    public void Init(TurnManager turnManager, BidManager bidManager)
    {
        this.turnManager = turnManager;
        this.bidManager = bidManager;
    }

    /// <summary>
    /// Called when it's an AI player's turn to bid.
    /// Uses AIStrategy to evaluate hand strength and decide whether to call/grab.
    /// Calling phase: call if hand score >= 8.
    /// Grabbing phase: grab if hand score >= 10 (higher bar since multiplier doubles).
    /// </summary>
    public void HandleBid(int playerIndex)
    {
        Player player = GameManager.Instance.Players[playerIndex];

        // Only AI players (index 1 and 2) are handled here
        if (playerIndex == 0)
            return;

        int handScore = AIStrategy.EvaluateHand(player.Hand);

        if (bidManager.CurrentPhase == BidManager.BidPhase.Calling)
        {
            // Calling phase: call landlord if hand is strong enough
            if (handScore >= 8)
            {
                Debug.Log($"[AI] {player.Name} calls landlord (hand score: {handScore}).");
                bidManager.CallLandlord();
            }
            else
            {
                Debug.Log($"[AI] {player.Name} passes on calling (hand score: {handScore}).");
                bidManager.Pass();
            }
        }
        else
        {
            // Grabbing phase: grab if hand is very strong (higher threshold)
            if (handScore >= 10)
            {
                Debug.Log($"[AI] {player.Name} grabs landlord! (hand score: {handScore}).");
                bidManager.GrabLandlord();
            }
            else
            {
                Debug.Log($"[AI] {player.Name} passes on grabbing (hand score: {handScore}).");
                bidManager.Pass();
            }
        }
    }

    /// <summary>
    /// Called when it's an AI player's turn to play cards.
    /// Builds a GameContext from the current game state and uses AIStrategy to choose the best play.
    /// </summary>
    public void HandlePlay(int playerIndex)
    {
        Player player = GameManager.Instance.Players[playerIndex];

        // Only AI players are handled here
        if (playerIndex == 0)
            return;

        // Build game context so AI can consider opponent card counts
        Player[] players = GameManager.Instance.Players;
        GameContext context = new GameContext
        {
            MyCardCount = player.Hand.Count,
            OpponentCounts = new int[2]
        };
        int idx = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (i != playerIndex)
                context.OpponentCounts[idx++] = players[i].Hand.Count;
        }

        CardCombo lastPlayed = turnManager.GetLastPlayedCombo();

        if (lastPlayed == null)
        {
            // Free play: AI leads with context-aware strategy
            CardCombo leadPlay = AIStrategy.ChooseLeadPlay(player.Hand, context);
            if (leadPlay != null)
            {
                Debug.Log($"[AI] {player.Name} leads with: {leadPlay}");
                turnManager.PlayCards(leadPlay.Cards);
            }
        }
        else
        {
            // Follow play: try to beat the last combo with context awareness
            CardCombo followPlay = AIStrategy.ChooseFollowPlay(player.Hand, lastPlayed, context);
            if (followPlay != null)
            {
                Debug.Log($"[AI] {player.Name} beats with: {followPlay}");
                turnManager.PlayCards(followPlay.Cards);
            }
            else
            {
                Debug.Log($"[AI] {player.Name} cannot beat {lastPlayed}, passes.");
                turnManager.Pass();
            }
        }
    }
}
