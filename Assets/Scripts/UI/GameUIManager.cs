using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all game UI elements: buttons, labels, played cards display.
/// Coordinates between player input and game flow managers.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    // References set by GameSetup
    private HandView handView;
    private TurnManager turnManager;
    private BidManager bidManager;
    private ScoreManager scoreManager;
    private AIPlayer aiPlayer;

    // UI elements (created by GameSetup)
    private Button playButton;
    private Button passButton;
    private Button bid1Button;
    private Button bid2Button;
    private Button bid3Button;
    private Button noBidButton;
    private GameObject bidPanel;
    private GameObject playPanel;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI lastPlayedText;
    private TextMeshProUGUI[] playerInfoTexts = new TextMeshProUGUI[3];
    private Button restartButton;

    /// <summary>
    /// Initializes references to other managers and UI components.
    /// </summary>
    public void Init(HandView handView, TurnManager turnManager, BidManager bidManager, ScoreManager scoreManager, AIPlayer aiPlayer)
    {
        this.handView = handView;
        this.turnManager = turnManager;
        this.bidManager = bidManager;
        this.scoreManager = scoreManager;
        this.aiPlayer = aiPlayer;

        // Wire score manager into turn manager for play tracking
        turnManager.SetScoreManager(scoreManager);
    }

    /// <summary>
    /// Sets references to UI elements created by GameSetup.
    /// </summary>
    public void SetUIElements(
        Button playBtn, Button passBtn,
        Button bid1Btn, Button bid2Btn, Button bid3Btn, Button noBidBtn,
        GameObject bidPnl, GameObject playPnl,
        TextMeshProUGUI msgText, TextMeshProUGUI lastPlayed,
        TextMeshProUGUI[] playerInfos, Button restartBtn)
    {
        playButton = playBtn;
        passButton = passBtn;
        bid1Button = bid1Btn;
        bid2Button = bid2Btn;
        bid3Button = bid3Btn;
        noBidButton = noBidBtn;
        bidPanel = bidPnl;
        playPanel = playPnl;
        messageText = msgText;
        lastPlayedText = lastPlayed;
        playerInfoTexts = playerInfos;
        restartButton = restartBtn;

        // Wire up button clicks
        playButton.onClick.AddListener(OnPlayClicked);
        passButton.onClick.AddListener(OnPassClicked);
        bid1Button.onClick.AddListener(() => OnBidScoreClicked(1));
        bid2Button.onClick.AddListener(() => OnBidScoreClicked(2));
        bid3Button.onClick.AddListener(() => OnBidScoreClicked(3));
        noBidButton.onClick.AddListener(OnNoBidClicked);
        restartButton.onClick.AddListener(OnRestartClicked);

        // Initially hide action panels and restart button
        bidPanel.SetActive(false);
        playPanel.SetActive(false);
        restartButton.gameObject.SetActive(false);
    }

    // ==================== Bidding Phase ====================

    /// <summary>
    /// Shows the bidding UI for the human player, or triggers AI bidding.
    /// </summary>
    public void StartBidding()
    {
        bidManager.StartBidding();
        ProcessBidTurn();
    }

    /// <summary>
    /// Processes the current bidder's turn. Shows UI for human, auto-plays for AI.
    /// </summary>
    private void ProcessBidTurn()
    {
        if (!bidManager.IsBidding())
            return;

        int currentBidder = bidManager.GetCurrentBidder();
        UpdatePlayerInfo();

        if (currentBidder == 0)
        {
            // Human player's turn to bid
            int highest = bidManager.GetHighestBid();
            SetMessage(highest > 0
                ? $"Current highest bid: {highest}. Your turn to bid."
                : "Your turn to bid.");
            bidPanel.SetActive(true);
            playPanel.SetActive(false);
            UpdateBidButtons();
        }
        else
        {
            // AI player's turn to bid
            bidPanel.SetActive(false);
            aiPlayer.HandleBid(currentBidder);

            // Check if bidding ended (someone bid or need to re-deal)
            if (!bidManager.IsBidding())
            {
                OnBiddingComplete();
            }
            else
            {
                // Continue to next bidder
                ProcessBidTurn();
            }
        }
    }

    private void OnBidScoreClicked(int score)
    {
        bidPanel.SetActive(false);
        bidManager.BidScore(score);

        if (!bidManager.IsBidding())
        {
            OnBiddingComplete();
        }
        else
        {
            ProcessBidTurn();
        }
    }

    private void OnNoBidClicked()
    {
        bidPanel.SetActive(false);
        bidManager.Pass();

        if (!bidManager.IsBidding())
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Bidding)
            {
                // All passed, game re-dealt via StartGame
                RefreshHand();
                StartBidding();
            }
            else
            {
                // Someone had bid earlier, they became landlord
                OnBiddingComplete();
            }
        }
        else
        {
            ProcessBidTurn();
        }
    }

    /// <summary>
    /// Enables/disables bid score buttons based on the current highest bid.
    /// Players can only bid higher than the current highest.
    /// </summary>
    private void UpdateBidButtons()
    {
        int highest = bidManager.GetHighestBid();
        bid1Button.interactable = highest < 1;
        bid2Button.interactable = highest < 2;
        bid3Button.interactable = highest < 3;
    }

    /// <summary>
    /// Called when bidding is complete and a landlord is assigned.
    /// Refreshes hand (landlord got 3 extra cards) and starts playing phase.
    /// </summary>
    private void OnBiddingComplete()
    {
        // Find landlord name
        Player landlord = GameManager.Instance.Players.First(p => p.IsLandlord);
        SetMessage($"{landlord.Name} is the landlord!");

        // Refresh human player's hand (may have gotten landlord cards)
        RefreshHand();
        UpdatePlayerInfo();

        // Initialize score tracking with the bid score
        scoreManager.ResetForNewGame(bidManager.FinalBidScore);

        // Start playing phase
        turnManager.StartPlaying();

        // Delay slightly then process first turn
        Invoke(nameof(ProcessPlayTurn), 0.5f);
    }

    // ==================== Playing Phase ====================

    /// <summary>
    /// Processes the current player's turn. Shows UI for human, auto-plays for AI.
    /// </summary>
    private void ProcessPlayTurn()
    {
        if (!turnManager.IsPlaying())
        {
            OnGameOver();
            return;
        }

        Player currentPlayer = turnManager.GetCurrentPlayer();
        UpdatePlayerInfo();
        UpdateLastPlayedDisplay();

        if (currentPlayer.Index == 0)
        {
            // Human player's turn
            SetMessage("Your turn. Select cards and play, or pass.");
            playPanel.SetActive(true);
            UpdatePlayButtons();
        }
        else
        {
            // AI player's turn
            playPanel.SetActive(false);
            SetMessage($"{currentPlayer.Name} is thinking...");

            // Small delay so the player can see what's happening
            Invoke(nameof(AIPlayTurn), 0.8f);
        }
    }

    /// <summary>
    /// Executes the AI player's turn with a delay for readability.
    /// </summary>
    private void AIPlayTurn()
    {
        if (!turnManager.IsPlaying())
            return;

        Player currentPlayer = turnManager.GetCurrentPlayer();
        aiPlayer.HandlePlay(currentPlayer.Index);

        UpdatePlayerInfo();
        UpdateLastPlayedDisplay();

        // Continue to next turn
        if (turnManager.IsPlaying())
        {
            ProcessPlayTurn();
        }
        else
        {
            OnGameOver();
        }
    }

    private void OnPlayClicked()
    {
        List<Card> selected = handView.GetSelectedCards();
        if (selected.Count == 0)
            return;

        bool success = turnManager.PlayCards(selected);
        if (success)
        {
            handView.RemoveCards(selected);
            playPanel.SetActive(false);
            UpdateLastPlayedDisplay();
            UpdatePlayerInfo();

            if (turnManager.IsPlaying())
            {
                ProcessPlayTurn();
            }
            else
            {
                OnGameOver();
            }
        }
        else
        {
            SetMessage("Invalid play! Try a different combination.");
            handView.DeselectAll();
        }
    }

    private void OnPassClicked()
    {
        bool success = turnManager.Pass();
        if (success)
        {
            handView.DeselectAll();
            playPanel.SetActive(false);
            UpdateLastPlayedDisplay();
            ProcessPlayTurn();
        }
        else
        {
            SetMessage("You must play - cannot pass on free play!");
        }
    }

    // ==================== Game Over ====================

    private void OnGameOver()
    {
        playPanel.SetActive(false);
        bidPanel.SetActive(false);

        // Find who won and calculate scores
        Player winner = GameManager.Instance.Players.FirstOrDefault(p => p.Hand.Count == 0);
        if (winner != null)
        {
            scoreManager.CalculateScores(winner.Index);

            // Build result message with score details
            string winMsg = winner.IsLandlord
                ? $"Game Over! {winner.Name} (Landlord) wins!"
                : $"Game Over! Farmers win! ({winner.Name} finished first)";

            string scoreMsg = "\n";
            for (int i = 0; i < 3; i++)
            {
                Player p = GameManager.Instance.Players[i];
                string change = ScoreManager.FormatScoreChange(scoreManager.LastRoundScores[i]);
                scoreMsg += $"{p.Name}: {change} (Total: {scoreManager.TotalScores[i]})  ";
            }

            SetMessage(winMsg + scoreMsg);
        }

        UpdatePlayerInfo();

        // Show restart button
        restartButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Restarts the game: clears UI, resets state, begins a new round.
    /// </summary>
    private void OnRestartClicked()
    {
        // Cancel any pending AI invokes
        CancelInvoke();

        // Hide restart button
        restartButton.gameObject.SetActive(false);

        // Clear hand display and last played text
        handView.ClearHand();
        lastPlayedText.text = "";

        // Start a fresh game
        BeginGame();
    }

    // ==================== UI Updates ====================

    /// <summary>
    /// Called when card selection changes. Updates play button state.
    /// </summary>
    public void OnSelectionChanged()
    {
        UpdatePlayButtons();
    }

    /// <summary>
    /// Enables/disables play and pass buttons based on current state.
    /// </summary>
    private void UpdatePlayButtons()
    {
        List<Card> selected = handView.GetSelectedCards();
        playButton.interactable = selected.Count > 0;

        // Can only pass if there's a previous combo to beat
        passButton.interactable = turnManager.GetLastPlayedCombo() != null;
    }

    /// <summary>
    /// Updates the display showing what was last played on the table.
    /// </summary>
    private void UpdateLastPlayedDisplay()
    {
        CardCombo lastCombo = turnManager.GetLastPlayedCombo();
        if (lastCombo != null)
        {
            string cardsStr = string.Join(" ", lastCombo.Cards.Select(c => FormatCard(c)));
            lastPlayedText.text = $"Last played: {cardsStr}\n({lastCombo.Type})";
        }
        else
        {
            lastPlayedText.text = "Free play";
        }
    }

    /// <summary>
    /// Updates player info labels (name, card count, landlord marker).
    /// </summary>
    private void UpdatePlayerInfo()
    {
        for (int i = 0; i < 3; i++)
        {
            Player p = GameManager.Instance.Players[i];
            string role = p.IsLandlord ? " [Landlord]" : "";
            string score = scoreManager != null ? $" | Score: {scoreManager.TotalScores[i]}" : "";
            playerInfoTexts[i].text = $"{p.Name}{role}\nCards: {p.Hand.Count}{score}";
        }
    }

    /// <summary>
    /// Sets the central message text.
    /// </summary>
    private void SetMessage(string msg)
    {
        messageText.text = msg;
    }

    /// <summary>
    /// Refreshes the hand view with the human player's current cards.
    /// </summary>
    private void RefreshHand()
    {
        handView.ShowHand(GameManager.Instance.Players[0].Hand);
    }

    /// <summary>
    /// Returns a short display string for a card (e.g. "♠A", "♥3").
    /// </summary>
    private string FormatCard(Card card)
    {
        string suit = "";
        switch (card.Suit)
        {
            case Suit.Spade: suit = "\u2660"; break;
            case Suit.Heart: suit = "\u2665"; break;
            case Suit.Diamond: suit = "\u2666"; break;
            case Suit.Club: suit = "\u2663"; break;
            default: suit = "\u2605"; break;
        }

        string rank = "";
        switch (card.Rank)
        {
            case Rank.Three: rank = "3"; break;
            case Rank.Four: rank = "4"; break;
            case Rank.Five: rank = "5"; break;
            case Rank.Six: rank = "6"; break;
            case Rank.Seven: rank = "7"; break;
            case Rank.Eight: rank = "8"; break;
            case Rank.Nine: rank = "9"; break;
            case Rank.Ten: rank = "10"; break;
            case Rank.Jack: rank = "J"; break;
            case Rank.Queen: rank = "Q"; break;
            case Rank.King: rank = "K"; break;
            case Rank.Ace: rank = "A"; break;
            case Rank.Two: rank = "2"; break;
            case Rank.BlackJoker: rank = "BJ"; break;
            case Rank.RedJoker: rank = "RJ"; break;
        }

        return suit + rank;
    }

    /// <summary>
    /// Starts a full game flow: deal, show hand, begin bidding.
    /// Called by GameSetup after everything is initialized.
    /// </summary>
    public void BeginGame()
    {
        GameManager.Instance.StartGame();
        RefreshHand();
        StartBidding();
    }
}
