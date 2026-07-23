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
    private AIPlayer aiPlayer;

    // UI elements (created by GameSetup)
    private Button playButton;
    private Button passButton;
    private Button bidButton;
    private Button noBidButton;
    private GameObject bidPanel;
    private GameObject playPanel;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI lastPlayedText;
    private TextMeshProUGUI[] playerInfoTexts = new TextMeshProUGUI[3];

    /// <summary>
    /// Initializes references to other managers and UI components.
    /// </summary>
    public void Init(HandView handView, TurnManager turnManager, BidManager bidManager, AIPlayer aiPlayer)
    {
        this.handView = handView;
        this.turnManager = turnManager;
        this.bidManager = bidManager;
        this.aiPlayer = aiPlayer;
    }

    /// <summary>
    /// Sets references to UI elements created by GameSetup.
    /// </summary>
    public void SetUIElements(
        Button playBtn, Button passBtn,
        Button bidBtn, Button noBidBtn,
        GameObject bidPnl, GameObject playPnl,
        TextMeshProUGUI msgText, TextMeshProUGUI lastPlayed,
        TextMeshProUGUI[] playerInfos)
    {
        playButton = playBtn;
        passButton = passBtn;
        bidButton = bidBtn;
        noBidButton = noBidBtn;
        bidPanel = bidPnl;
        playPanel = playPnl;
        messageText = msgText;
        lastPlayedText = lastPlayed;
        playerInfoTexts = playerInfos;

        // Wire up button clicks
        playButton.onClick.AddListener(OnPlayClicked);
        passButton.onClick.AddListener(OnPassClicked);
        bidButton.onClick.AddListener(OnBidClicked);
        noBidButton.onClick.AddListener(OnNoBidClicked);

        // Initially hide action panels
        bidPanel.SetActive(false);
        playPanel.SetActive(false);
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
            SetMessage("Your turn to bid. Call landlord?");
            bidPanel.SetActive(true);
            playPanel.SetActive(false);
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

    private void OnBidClicked()
    {
        bidPanel.SetActive(false);
        bidManager.Bid();
        OnBiddingComplete();
    }

    private void OnNoBidClicked()
    {
        bidPanel.SetActive(false);
        bidManager.Pass();

        if (!bidManager.IsBidding())
        {
            // All passed, game will re-deal via StartGame
            // Re-show hand and restart bidding
            RefreshHand();
            StartBidding();
        }
        else
        {
            ProcessBidTurn();
        }
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

        // Find who won
        Player winner = GameManager.Instance.Players.FirstOrDefault(p => p.Hand.Count == 0);
        if (winner != null)
        {
            if (winner.IsLandlord)
                SetMessage($"Game Over! {winner.Name} (Landlord) wins!");
            else
                SetMessage($"Game Over! Farmers win! ({winner.Name} finished first)");
        }
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
            playerInfoTexts[i].text = $"{p.Name}{role}\nCards: {p.Hand.Count}";
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
