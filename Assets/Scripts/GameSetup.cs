using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bootstraps the entire game scene programmatically.
/// Creates Canvas, UI elements, and all manager components.
///
/// Usage: Attach this script to an empty GameObject in the scene.
/// Everything else is created automatically at runtime.
/// </summary>
public class GameSetup : MonoBehaviour
{
    // Cached Chinese font asset for all TMP text elements
    private TMP_FontAsset chineseFont;

    // Shared canvas used by both menu and game
    private GameObject canvasObj;

    // Main menu panel - hidden when game starts, shown when returning to menu
    private GameObject menuPanel;

    // All game objects created by CreateGame, stored for cleanup when returning to menu
    private GameObject gameObjectsRoot;

    private void Start()
    {
        SetupSharedResources();
        CreateMainMenu();
    }

    /// <summary>
    /// Initializes font, canvas, and event system shared by menu and game.
    /// </summary>
    private void SetupSharedResources()
    {
        // Load Chinese font
        Font notoFont = Resources.Load<Font>("NotoSansSC-Regular");
        if (notoFont != null)
        {
            chineseFont = TMP_FontAsset.CreateFontAsset(notoFont);
        }
        else
        {
            Debug.LogWarning("NotoSansSC-Regular.ttf not found in Resources. Chinese text will not display.");
        }

        // Create the main UI canvas
        canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event system for UI interaction
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    /// <summary>
    /// Creates the main menu with background, title, and start/quit buttons.
    /// </summary>
    private void CreateMainMenu()
    {
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform menuRect = menuPanel.AddComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        // Full-screen menu background image
        GameObject bgObj = new GameObject("MenuBackground");
        bgObj.transform.SetParent(menuPanel.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.raycastTarget = false;

        Texture2D menuBgTex = Resources.Load<Texture2D>("Sprites/menu_background");
        if (menuBgTex != null)
        {
            bgImage.sprite = Sprite.Create(menuBgTex,
                new Rect(0, 0, menuBgTex.width, menuBgTex.height),
                new Vector2(0.5f, 0.5f));
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
        }
        else
        {
            bgImage.color = new Color(0.1f, 0.08f, 0.06f);
            Debug.LogWarning("Menu background image not found: Sprites/menu_background");
        }

        // Game logo image (calligraphy 水浒传 / 斗地主)
        GameObject logoObj = new GameObject("MenuLogo");
        logoObj.transform.SetParent(menuPanel.transform, false);
        RectTransform logoRect = logoObj.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.65f);
        logoRect.anchorMax = new Vector2(0.5f, 0.65f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.sizeDelta = new Vector2(600, 270);
        Image logoImg = logoObj.AddComponent<Image>();
        logoImg.raycastTarget = false;

        Texture2D logoTex = Resources.Load<Texture2D>("Sprites/menu_logo");
        if (logoTex != null)
        {
            logoImg.sprite = Sprite.Create(logoTex,
                new Rect(0, 0, logoTex.width, logoTex.height),
                new Vector2(0.5f, 0.5f));
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("Menu logo not found: Sprites/menu_logo");
        }

        // Subtitle below logo - can be localized independently
        TextMeshProUGUI subtitleText = CreateText(menuPanel.transform, "SubtitleText",
            "Water Margin Landlords",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600, 40), 22);
        subtitleText.color = new Color(0.85f, 0.75f, 0.5f);
        subtitleText.outlineWidth = 0.25f;
        subtitleText.outlineColor = new Color32(20, 15, 10, 255);

        // Start Game button
        Button startButton = CreateButton(menuPanel.transform, "StartButton",
            "\u5f00\u59cb\u6e38\u620f", Vector2.zero, new Vector2(240, 55));  // 开始游戏
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0.4f);
        startRect.anchorMax = new Vector2(0.5f, 0.4f);
        startRect.anchoredPosition = Vector2.zero;

        startButton.onClick.AddListener(() =>
        {
            menuPanel.SetActive(false);
            CreateGame();
        });

        // Quit Game button
        Button quitButton = CreateButton(menuPanel.transform, "QuitButton",
            "\u9000\u51fa\u6e38\u620f", Vector2.zero, new Vector2(240, 55));  // 退出游戏
        RectTransform quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0.32f);
        quitRect.anchorMax = new Vector2(0.5f, 0.32f);
        quitRect.anchoredPosition = Vector2.zero;

        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    /// <summary>
    /// Destroys all game objects and returns to the main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Restore time scale in case we came from pause
        Time.timeScale = 1f;

        // Destroy all game-specific objects
        if (gameObjectsRoot != null)
        {
            Destroy(gameObjectsRoot);
            gameObjectsRoot = null;
        }

        // Destroy GameManagers (singletons etc.)
        GameObject managers = GameObject.Find("GameManagers");
        if (managers != null) Destroy(managers);

        // Show the main menu again
        menuPanel.SetActive(true);
    }

    /// <summary>
    /// Creates all game objects, managers, and UI elements.
    /// </summary>
    private void CreateGame()
    {
        // Container for all game objects so we can destroy them when returning to menu
        gameObjectsRoot = new GameObject("GameObjects");
        gameObjectsRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = gameObjectsRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // ==================== Managers ====================

        // Create a central manager object with all game flow components
        GameObject managerObj = new GameObject("GameManagers");
        GameManager gameManager = managerObj.AddComponent<GameManager>();
        TurnManager turnManager = managerObj.AddComponent<TurnManager>();
        BidManager bidManager = managerObj.AddComponent<BidManager>();
        ScoreManager scoreManager = managerObj.AddComponent<ScoreManager>();
        AIPlayer aiPlayer = managerObj.AddComponent<AIPlayer>();

        // ==================== Background ====================

        // Load Water Margin ink wash background image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.raycastTarget = false;  // Don't block clicks on cards

        Texture2D bgTex = Resources.Load<Texture2D>("Sprites/background");
        if (bgTex != null)
        {
            bgImage.sprite = Sprite.Create(bgTex,
                new Rect(0, 0, bgTex.width, bgTex.height),
                new Vector2(0.5f, 0.5f));
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;  // Stretch to fill screen
        }
        else
        {
            // Fallback to solid color if image not found
            bgImage.color = new Color(0.1f, 0.08f, 0.06f);
            Debug.LogWarning("Background image not found: Sprites/background");
        }

        // ==================== Player Hand Area (Bottom) ====================

        GameObject handArea = new GameObject("HandArea");
        handArea.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform handRect = handArea.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0);
        handRect.anchorMax = new Vector2(0.5f, 0);
        handRect.pivot = new Vector2(0.5f, 0);
        handRect.anchoredPosition = new Vector2(0, 30);
        handRect.sizeDelta = new Vector2(1600, 180);
        HandView handView = handArea.AddComponent<HandView>();

        // ==================== Player Info Labels ====================

        // Player 0 (Human) - bottom center, above hand area
        TextMeshProUGUI playerInfo0 = CreateText(gameObjectsRoot.transform, "PlayerInfo_You",
            "\u4f60",  // 你
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 220), new Vector2(300, 30), 14);

        // Player 1 (AI Left) - top left corner
        TextMeshProUGUI playerInfo1 = CreateText(gameObjectsRoot.transform, "PlayerInfo_Left",
            "\u6797\u51b2 | \u624b\u724c: 17",  // 林冲 | 手牌: 17
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(120, -30), new Vector2(220, 30), 16);

        // Player 1 (AI Left) card backs area - below info label
        GameObject aiCards1 = new GameObject("AICards_Left");
        aiCards1.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform aiCards1Rect = aiCards1.AddComponent<RectTransform>();
        aiCards1Rect.anchorMin = new Vector2(0, 1);
        aiCards1Rect.anchorMax = new Vector2(0, 1);
        aiCards1Rect.pivot = new Vector2(0, 1);
        aiCards1Rect.anchoredPosition = new Vector2(30, -55);
        aiCards1Rect.sizeDelta = new Vector2(200, 50);

        // Player 2 (AI Right) - top right corner
        TextMeshProUGUI playerInfo2 = CreateText(gameObjectsRoot.transform, "PlayerInfo_Right",
            "\u9c81\u667a\u6df1 | \u624b\u724c: 17",  // 鲁智深 | 手牌: 17
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-130, -30), new Vector2(240, 30), 16);

        // Player 2 (AI Right) card backs area - below info label
        GameObject aiCards2 = new GameObject("AICards_Right");
        aiCards2.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform aiCards2Rect = aiCards2.AddComponent<RectTransform>();
        aiCards2Rect.anchorMin = new Vector2(1, 1);
        aiCards2Rect.anchorMax = new Vector2(1, 1);
        aiCards2Rect.pivot = new Vector2(1, 1);
        aiCards2Rect.anchoredPosition = new Vector2(-30, -55);
        aiCards2Rect.sizeDelta = new Vector2(200, 50);

        TextMeshProUGUI[] playerInfoTexts = { playerInfo0, playerInfo1, playerInfo2 };

        // ==================== Played Cards Areas (inverted triangle layout) ====================

        // Player 0 (Human) played cards - center, below AI played areas
        GameObject playedArea0 = new GameObject("PlayedCards_You");
        playedArea0.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform played0Rect = playedArea0.AddComponent<RectTransform>();
        played0Rect.anchorMin = new Vector2(0.5f, 0.38f);
        played0Rect.anchorMax = new Vector2(0.5f, 0.38f);
        played0Rect.pivot = new Vector2(0.5f, 0.5f);
        played0Rect.anchoredPosition = Vector2.zero;
        played0Rect.sizeDelta = new Vector2(900, 160);

        // Player 1 (AI Left) played cards - center-left, closer to middle
        GameObject playedArea1 = new GameObject("PlayedCards_Left");
        playedArea1.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform played1Rect = playedArea1.AddComponent<RectTransform>();
        played1Rect.anchorMin = new Vector2(0.35f, 0.65f);
        played1Rect.anchorMax = new Vector2(0.35f, 0.65f);
        played1Rect.pivot = new Vector2(0.5f, 0.5f);
        played1Rect.anchoredPosition = Vector2.zero;
        played1Rect.sizeDelta = new Vector2(400, 150);

        // Player 2 (AI Right) played cards - center-right, closer to middle
        GameObject playedArea2 = new GameObject("PlayedCards_Right");
        playedArea2.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform played2Rect = playedArea2.AddComponent<RectTransform>();
        played2Rect.anchorMin = new Vector2(0.65f, 0.65f);
        played2Rect.anchorMax = new Vector2(0.65f, 0.65f);
        played2Rect.pivot = new Vector2(0.5f, 0.5f);
        played2Rect.anchoredPosition = Vector2.zero;
        played2Rect.sizeDelta = new Vector2(400, 150);

        Transform[] playedAreas = { playedArea0.transform, playedArea1.transform, playedArea2.transform };

        // ==================== Center Area ====================

        // Message text (top center)
        TextMeshProUGUI messageText = CreateText(gameObjectsRoot.transform, "MessageText",
            "\u6b22\u8fce\u6765\u5230\u6c34\u6d52\u4f20\u6597\u5730\u4e3b\uff01",  // 欢迎来到水浒传斗地主！
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f),
            Vector2.zero, new Vector2(600, 40), 20);

        // Combo type label (center, between played areas)
        TextMeshProUGUI lastPlayedText = CreateText(gameObjectsRoot.transform, "LastPlayedText",
            "",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, new Vector2(300, 30), 16);

        // ==================== Bid Panel ====================

        GameObject bidPanel = new GameObject("BidPanel");
        bidPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform bidPanelRect = bidPanel.AddComponent<RectTransform>();
        bidPanelRect.anchorMin = new Vector2(0.5f, 0);
        bidPanelRect.anchorMax = new Vector2(0.5f, 0);
        bidPanelRect.anchoredPosition = new Vector2(0, 260);
        bidPanelRect.sizeDelta = new Vector2(700, 50);

        Button bid1Button = CreateButton(bidPanel.transform, "Bid1Button",
            "1\u5206", new Vector2(-240, 0), new Vector2(140, 45));   // 1分
        Button bid2Button = CreateButton(bidPanel.transform, "Bid2Button",
            "2\u5206", new Vector2(-80, 0), new Vector2(140, 45));   // 2分
        Button bid3Button = CreateButton(bidPanel.transform, "Bid3Button",
            "3\u5206", new Vector2(80, 0), new Vector2(140, 45));    // 3分
        Button noBidButton = CreateButton(bidPanel.transform, "NoBidButton",
            "\u4e0d\u53eb", new Vector2(240, 0), new Vector2(140, 45));  // 不叫

        // ==================== Play Panel ====================

        GameObject playPanel = new GameObject("PlayPanel");
        playPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform playPanelRect = playPanel.AddComponent<RectTransform>();
        playPanelRect.anchorMin = new Vector2(0.5f, 0);
        playPanelRect.anchorMax = new Vector2(0.5f, 0);
        playPanelRect.anchoredPosition = new Vector2(0, 260);
        playPanelRect.sizeDelta = new Vector2(400, 50);

        Button playButton = CreateButton(playPanel.transform, "PlayButton",
            "\u51fa\u724c", new Vector2(-110, 0), new Vector2(160, 45));  // 出牌
        Button passButton = CreateButton(playPanel.transform, "PassButton",
            "\u4e0d\u51fa", new Vector2(110, 0), new Vector2(160, 45));  // 不出

        // ==================== Restart Button ====================

        Button restartButton = CreateButton(gameObjectsRoot.transform, "RestartButton",
            "\u518d\u6765\u4e00\u5c40", Vector2.zero, new Vector2(200, 50));  // 再来一局
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = Vector2.zero;

        // ==================== Side Panels ====================

        // Skill panel (right side) - placeholder frame
        GameObject skillPanel = new GameObject("SkillPanel");
        skillPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform skillRect = skillPanel.AddComponent<RectTransform>();
        skillRect.anchorMin = new Vector2(1, 0);
        skillRect.anchorMax = new Vector2(1, 0);
        skillRect.pivot = new Vector2(1, 0);
        skillRect.anchoredPosition = new Vector2(-15, 30);
        skillRect.sizeDelta = new Vector2(120, 160);
        Image skillBg = skillPanel.AddComponent<Image>();
        skillBg.color = new Color(0.1f, 0.08f, 0.06f, 0.8f);

        Outline skillOutline = skillPanel.AddComponent<Outline>();
        skillOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        skillOutline.effectDistance = new Vector2(2, 2);

        // Skill panel title
        CreateText(skillPanel.transform, "SkillTitle", "\u6280\u80fd",  // 技能
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -18), new Vector2(130, 25), 14);

        // ==================== Pause Menu ====================

        // Full-screen semi-transparent dark overlay
        GameObject pauseOverlay = CreatePanel(gameObjectsRoot.transform, "PauseOverlay",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.7f));

        // Center panel background
        GameObject pauseCenter = new GameObject("PauseCenter");
        pauseCenter.transform.SetParent(pauseOverlay.transform, false);
        RectTransform pauseCenterRect = pauseCenter.AddComponent<RectTransform>();
        pauseCenterRect.anchorMin = new Vector2(0.5f, 0.5f);
        pauseCenterRect.anchorMax = new Vector2(0.5f, 0.5f);
        pauseCenterRect.sizeDelta = new Vector2(350, 360);
        Image pauseBg = pauseCenter.AddComponent<Image>();
        pauseBg.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);

        // "PAUSED" title
        CreateText(pauseCenter.transform, "PausedTitle", "\u6682\u505c",  // 暂停
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -40), new Vector2(300, 50), 32);

        // Resume, Restart, Return to Menu, Quit buttons
        Button resumeButton = CreateButton(pauseCenter.transform, "ResumeButton",
            "\u7ee7\u7eed\u6e38\u620f", new Vector2(0, 50), new Vector2(220, 50));  // 继续游戏
        Button pauseRestartButton = CreateButton(pauseCenter.transform, "PauseRestartButton",
            "\u91cd\u65b0\u5f00\u59cb", new Vector2(0, -10), new Vector2(220, 50));  // 重新开始
        Button mainMenuButton = CreateButton(pauseCenter.transform, "MainMenuButton",
            "\u8fd4\u56de\u4e3b\u83dc\u5355", new Vector2(0, -70), new Vector2(220, 50));  // 返回主菜单
        Button quitButton = CreateButton(pauseCenter.transform, "QuitButton",
            "\u9000\u51fa\u6e38\u620f", new Vector2(0, -130), new Vector2(220, 50));  // 退出游戏

        // Start hidden
        pauseOverlay.SetActive(false);

        // ==================== Wire Everything Up ====================

        aiPlayer.Init(turnManager, bidManager);
        GameUIManager uiManager = gameObjectsRoot.AddComponent<GameUIManager>();
        handView.Init(uiManager);
        uiManager.Init(handView, turnManager, bidManager, scoreManager, aiPlayer);
        Transform[] aiCardAreas = { aiCards1.transform, aiCards2.transform };
        uiManager.SetUIElements(
            playButton, passButton,
            bid1Button, bid2Button, bid3Button, noBidButton,
            bidPanel, playPanel,
            messageText, lastPlayedText,
            playerInfoTexts, restartButton,
            playedAreas, aiCardAreas
        );

        // Wire pause panel
        uiManager.SetPausePanel(pauseOverlay, resumeButton, pauseRestartButton, quitButton);

        // Wire "return to main menu" button - calls ReturnToMainMenu on this GameSetup
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Start the game
        uiManager.BeginGame();
    }

    // ==================== UI Factory Methods ====================

    /// <summary>
    /// Creates a full-screen colored panel.
    /// </summary>
    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    /// <summary>
    /// Creates a TextMeshPro text element.
    /// </summary>
    private TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (chineseFont != null) tmp.font = chineseFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    /// <summary>
    /// Creates a clickable button with text label.
    /// </summary>
    private Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.12f, 0.08f, 0.9f); // Dark brown background

        // Gold border outline
        Outline btnOutline = obj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.7f, 0.6f, 0.35f); // Gold border
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.12f, 0.08f, 0.9f);
        colors.highlightedColor = new Color(0.25f, 0.2f, 0.12f, 0.95f);
        colors.pressedColor = new Color(0.1f, 0.08f, 0.05f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        btn.colors = colors;

        // Button label text - gold color to match theme
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (chineseFont != null) tmp.font = chineseFont;
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.85f, 0.55f); // Gold text

        return btn;
    }
}
