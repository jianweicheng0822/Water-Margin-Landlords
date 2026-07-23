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
    /// Uses AIStrategy to evaluate hand strength and decide.
    /// </summary>
    public void HandleBid(int playerIndex)
    {
        Player player = GameManager.Instance.Players[playerIndex];

        // Only AI players (index 1 and 2) are handled here
        if (playerIndex == 0)
            return;

        bool shouldBid = AIStrategy.ShouldBid(player.Hand);

        if (shouldBid)
        {
            Debug.Log($"[AI] {player.Name} decides to bid landlord (score: {AIStrategy.EvaluateHand(player.Hand)}).");
            bidManager.Bid();
        }
        else
        {
            Debug.Log($"[AI] {player.Name} decides to pass (score: {AIStrategy.EvaluateHand(player.Hand)}).");
            bidManager.Pass();
        }
    }

    /// <summary>
    /// Called when it's an AI player's turn to play cards.
    /// Uses AIStrategy to choose the best play.
    /// </summary>
    public void HandlePlay(int playerIndex)
    {
        Player player = GameManager.Instance.Players[playerIndex];

        // Only AI players are handled here
        if (playerIndex == 0)
            return;

        CardCombo lastPlayed = turnManager.GetLastPlayedCombo();

        if (lastPlayed == null)
        {
            // Free play: AI leads with smallest combo
            CardCombo leadPlay = AIStrategy.ChooseLeadPlay(player.Hand);
            if (leadPlay != null)
            {
                Debug.Log($"[AI] {player.Name} leads with: {leadPlay}");
                turnManager.PlayCards(leadPlay.Cards);
            }
        }
        else
        {
            // Follow play: try to beat the last combo
            CardCombo followPlay = AIStrategy.ChooseFollowPlay(player.Hand, lastPlayed);
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
