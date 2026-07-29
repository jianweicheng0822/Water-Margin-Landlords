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

    // Played cards display areas (one per player, positioned above each player)
    private Transform[] playedCardAreas;

    // AI card back display areas (shows small card backs representing AI hand size)
    private Transform[] aiCardBackAreas;

    // Display card size for played cards
    private static readonly float PLAYED_CARD_WIDTH = 100f;
    private static readonly float PLAYED_CARD_HEIGHT = 150f;
    private static readonly float PLAYED_CARD_SPACING = 40f;

    // AI card back display size
    private static readonly float AI_CARD_BACK_WIDTH = 25f;
    private static readonly float AI_CARD_BACK_HEIGHT = 38f;
    private static readonly float AI_CARD_BACK_SPACING = 12f;

    // Tracks which players passed (to show "不出" text)
    private bool[] playerPassed = new bool[3];

    // Cached Chinese font for dynamically created text
    private TMP_FontAsset cachedChineseFont;

    // Cached card back sprite
    private Sprite cachedCardBackSprite;

    // Pause menu state
    private GameObject pausePanel;
    private bool isPaused;

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
        TextMeshProUGUI[] playerInfos, Button restartBtn,
        Transform[] playedAreas, Transform[] aiCardAreas)
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
        playedCardAreas = playedAreas;
        aiCardBackAreas = aiCardAreas;

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

    /// <summary>
    /// Sets the pause panel reference and wires up its button listeners.
    /// Called by GameSetup after creating the pause overlay.
    /// </summary>
    public void SetPausePanel(GameObject panel, Button resumeBtn, Button pauseRestartBtn, Button quitBtn)
    {
        pausePanel = panel;
        resumeBtn.onClick.AddListener(TogglePause);
        pauseRestartBtn.onClick.AddListener(() =>
        {
            TogglePause();
            OnRestartClicked();
        });
        quitBtn.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    /// <summary>
    /// Checks for ESC key press each frame to toggle the pause menu.
    /// Uses new Input System's Keyboard class.
    /// </summary>
    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            GamePhase phase = GameManager.Instance.CurrentPhase;
            if (phase == GamePhase.Bidding || phase == GamePhase.Playing)
            {
                TogglePause();
            }
        }
    }

    /// <summary>
    /// Toggles the pause state: shows/hides the pause overlay and freezes/unfreezes time.
    /// </summary>
    private void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
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
                ? $"\u5f53\u524d\u6700\u9ad8\u53eb\u5206\uff1a{highest}\u5206\u3002\u8bf7\u53eb\u5206\u3002"  // 当前最高叫分：X分。请叫分。
                : "\u8bf7\u53eb\u5206\u3002");  // 请叫分。
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
        SetMessage($"{landlord.Name} \u662f\u5730\u4e3b\uff01");  // X 是地主！

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
            SetMessage("\u8f6e\u5230\u4f60\u51fa\u724c\u3002\u9009\u62e9\u724c\u540e\u70b9\u51fa\u724c\u6216\u4e0d\u51fa\u3002");  // 轮到你出牌。选择牌后点出牌或不出。
            playPanel.SetActive(true);
            UpdatePlayButtons();
        }
        else
        {
            // AI player's turn
            playPanel.SetActive(false);
            SetMessage($"{currentPlayer.Name} \u6b63\u5728\u601d\u8003...");  // X 正在思考...

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
        int prevCardCount = currentPlayer.Hand.Count;
        aiPlayer.HandlePlay(currentPlayer.Index);

        // Check if AI passed (card count unchanged) or played
        if (currentPlayer.Hand.Count == prevCardCount)
        {
            playerPassed[currentPlayer.Index] = true;
        }
        else
        {
            playerPassed = new bool[3];  // Reset pass states when cards are played
        }

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
            playerPassed = new bool[3];  // Reset pass states when cards are played
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
            SetMessage("\u65e0\u6548\u7684\u51fa\u724c\uff01\u8bf7\u91cd\u65b0\u9009\u62e9\u3002");  // 无效的出牌！请重新选择。
            handView.DeselectAll();
        }
    }

    private void OnPassClicked()
    {
        bool success = turnManager.Pass();
        if (success)
        {
            playerPassed[0] = true;
            handView.DeselectAll();
            playPanel.SetActive(false);
            UpdateLastPlayedDisplay();
            ProcessPlayTurn();
        }
        else
        {
            SetMessage("\u5fc5\u987b\u51fa\u724c\uff01\u81ea\u7531\u51fa\u724c\u4e0d\u80fd\u8df3\u8fc7\u3002");  // 必须出牌！自由出牌不能跳过。
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
                ? $"\u6e38\u620f\u7ed3\u675f\uff01{winner.Name}\uff08\u5730\u4e3b\uff09\u83dc\uff01"   // 游戏结束！X（地主）赢！
                : $"\u6e38\u620f\u7ed3\u675f\uff01\u519c\u6c11\u83dc\uff01\uff08{winner.Name}\u5148\u51fa\u5b8c\uff09";  // 游戏结束！农民赢！（X先出完）

            string scoreMsg = "\n";
            for (int i = 0; i < 3; i++)
            {
                Player p = GameManager.Instance.Players[i];
                string change = ScoreManager.FormatScoreChange(scoreManager.LastRoundScores[i]);
                scoreMsg += $"{p.Name}: {change} (\u603b\u5206: {scoreManager.TotalScores[i]})  ";  // 总分
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

        // Reset pause state in case restarting from pause menu
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Hide restart button
        restartButton.gameObject.SetActive(false);

        // Clear hand display, played cards, card backs, and last played text
        handView.ClearHand();
        ClearAllPlayedAreas();
        lastPlayedText.text = "";

        // Clear AI card backs
        if (aiCardBackAreas != null)
        {
            foreach (Transform area in aiCardBackAreas)
            {
                for (int i = area.childCount - 1; i >= 0; i--)
                    Destroy(area.GetChild(i).gameObject);
            }
        }

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
    /// Updates the played cards display with card images above the player who played.
    /// Shows "不出" text for players who passed.
    /// </summary>
    private void UpdateLastPlayedDisplay()
    {
        // Clear all played card areas first
        ClearAllPlayedAreas();

        CardCombo lastCombo = turnManager.GetLastPlayedCombo();
        if (lastCombo != null)
        {
            int playerIndex = turnManager.GetLastPlayedBy();
            ShowPlayedCards(playerIndex, lastCombo.Cards);
            lastPlayedText.text = $"({GetComboTypeName(lastCombo.Type)})";

            // Show "不出" for players who passed
            for (int i = 0; i < 3; i++)
            {
                if (playerPassed[i])
                    ShowPassText(i);
            }
        }
        else
        {
            // Free play - reset all pass states
            playerPassed = new bool[3];
            lastPlayedText.text = "\u81ea\u7531\u51fa\u724c";  // 自由出牌
        }
    }

    /// <summary>
    /// Clears all card images from all played card areas.
    /// </summary>
    private void ClearAllPlayedAreas()
    {
        if (playedCardAreas == null) return;
        foreach (Transform area in playedCardAreas)
        {
            for (int i = area.childCount - 1; i >= 0; i--)
            {
                Destroy(area.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Shows card images in the specified player's played area.
    /// Cards are laid out horizontally, centered in the area.
    /// </summary>
    private void ShowPlayedCards(int playerIndex, List<Card> cards)
    {
        if (playedCardAreas == null || playerIndex < 0 || playerIndex >= playedCardAreas.Length)
            return;

        Transform area = playedCardAreas[playerIndex];
        float totalWidth = (cards.Count - 1) * PLAYED_CARD_SPACING + PLAYED_CARD_WIDTH;
        float startX = -totalWidth / 2f + PLAYED_CARD_WIDTH / 2f;

        for (int i = 0; i < cards.Count; i++)
        {
            GameObject cardObj = CardView.CreateDisplayCard(cards[i], area, PLAYED_CARD_WIDTH, PLAYED_CARD_HEIGHT);
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * PLAYED_CARD_SPACING, 0);
        }
    }

    /// <summary>
    /// Shows "不出" text in the specified player's played area.
    /// </summary>
    private void ShowPassText(int playerIndex)
    {
        if (playedCardAreas == null || playerIndex < 0 || playerIndex >= playedCardAreas.Length)
            return;

        Transform area = playedCardAreas[playerIndex];
        GameObject textObj = new GameObject("PassText");
        textObj.transform.SetParent(area, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        // Use cached Chinese font
        if (cachedChineseFont == null)
        {
            Font notoFont = Resources.Load<Font>("NotoSansSC-Regular");
            if (notoFont != null)
                cachedChineseFont = TMP_FontAsset.CreateFontAsset(notoFont);
        }
        if (cachedChineseFont != null)
            tmp.font = cachedChineseFont;
        tmp.text = "\u4e0d\u51fa";  // 不出
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.9f, 0.6f);  // Gold color
    }

    /// <summary>
    /// Updates player info labels (name, card count, landlord marker).
    /// </summary>
    private void UpdatePlayerInfo()
    {
        for (int i = 0; i < 3; i++)
        {
            Player p = GameManager.Instance.Players[i];
            string role = p.IsLandlord ? " [\u5730\u4e3b]" : "";  // [地主]
            // Name is displayed separately on the player info card,
            // so the info text only shows role badge and card count
            playerInfoTexts[i].text = $"{role}\u624b\u724c: {p.Hand.Count}";
        }

        // Update AI card back displays
        UpdateAICardBacks();
    }

    /// <summary>
    /// Updates AI card back displays to show how many cards each AI player holds.
    /// Uses small card_back images laid out horizontally.
    /// </summary>
    private void UpdateAICardBacks()
    {
        if (aiCardBackAreas == null) return;

        for (int i = 0; i < aiCardBackAreas.Length; i++)
        {
            Transform area = aiCardBackAreas[i];

            // Clear existing card backs
            for (int c = area.childCount - 1; c >= 0; c--)
                Destroy(area.GetChild(c).gameObject);

            // AI player indices are 1 and 2 (area index 0=player1, 1=player2)
            int playerIndex = i + 1;
            Player p = GameManager.Instance.Players[playerIndex];
            int cardCount = p.Hand.Count;

            // Show card backs with tight overlap
            float totalWidth = (cardCount - 1) * AI_CARD_BACK_SPACING + AI_CARD_BACK_WIDTH;
            float startX = 0;  // Start from left edge

            for (int j = 0; j < cardCount; j++)
            {
                GameObject backObj = new GameObject($"CardBack_{j}");
                backObj.transform.SetParent(area, false);
                RectTransform rect = backObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(AI_CARD_BACK_WIDTH, AI_CARD_BACK_HEIGHT);
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                rect.anchoredPosition = new Vector2(startX + j * AI_CARD_BACK_SPACING, 0);

                Image img = backObj.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = GetCardBackSprite();
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
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
    /// Returns the cached card back sprite, loading it on first call.
    /// </summary>
    private Sprite GetCardBackSprite()
    {
        if (cachedCardBackSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("Sprites/card_back");
            if (tex != null)
            {
                cachedCardBackSprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
        return cachedCardBackSprite;
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
    /// Returns the Chinese name for a combo type.
    /// </summary>
    private string GetComboTypeName(ComboType type)
    {
        switch (type)
        {
            case ComboType.Single: return "\u5355\u5f20";              // 单张
            case ComboType.Pair: return "\u5bf9\u5b50";                // 对子
            case ComboType.Triple: return "\u4e09\u6761";              // 三条
            case ComboType.TripleWithSingle: return "\u4e09\u5e26\u4e00";  // 三带一
            case ComboType.TripleWithPair: return "\u4e09\u5e26\u4e8c";    // 三带二
            case ComboType.Straight: return "\u987a\u5b50";            // 顺子
            case ComboType.StraightPair: return "\u8fde\u5bf9";        // 连对
            case ComboType.Plane: return "\u98de\u673a";               // 飞机
            case ComboType.PlaneWithSingles: return "\u98de\u673a\u5e26\u7fc5\u8180";  // 飞机带翅膀
            case ComboType.PlaneWithPairs: return "\u98de\u673a\u5e26\u5bf9";          // 飞机带对
            case ComboType.FourWithTwo: return "\u56db\u5e26\u4e8c";   // 四带二
            case ComboType.FourWithTwoPairs: return "\u56db\u5e26\u4e24\u5bf9";  // 四带两对
            case ComboType.Bomb: return "\u70b8\u5f39";                // 炸弹
            case ComboType.Rocket: return "\u706b\u7bad";              // 火箭
            default: return type.ToString();
        }
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
