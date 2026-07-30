using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    // Cached button background sprite for all themed buttons
    private Sprite buttonSprite;

    // Background music audio sources — menu and game use different BGM
    private AudioSource menuBgmSource;
    private AudioSource gameBgmSource;

    // Shared canvas used by both menu and game
    private GameObject canvasObj;

    // Main menu panel - hidden when game starts, shown when returning to menu
    private GameObject menuPanel;

    // All game objects created by CreateGame, stored for cleanup when returning to menu
    private GameObject gameObjectsRoot;

    // Settings panel overlay — shown when "设置" button is clicked
    private GameObject settingsOverlay;

    // Resolution options for the settings dropdown
    private readonly List<Vector2Int> resolutionOptions = new List<Vector2Int>
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440)
    };

    // Current resolution index in the resolutionOptions list
    private int currentResolutionIndex = 3; // Default 1920x1080

    private void Start()
    {
        // Initialize sound manager singleton (persists across scenes)
        if (SoundManager.Instance == null)
        {
            GameObject soundObj = new GameObject("SoundManager");
            soundObj.AddComponent<SoundManager>();
        }

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

        // Load button background sprite
        Texture2D btnTex = Resources.Load<Texture2D>("Sprites/button");
        if (btnTex != null)
        {
            buttonSprite = Sprite.Create(btnTex,
                new Rect(0, 0, btnTex.width, btnTex.height),
                new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning("Button background not found: Sprites/button");
        }

        // Load persisted settings before creating audio/display
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.3f);
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 3);
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        // Apply saved resolution and fullscreen mode
        if (currentResolutionIndex >= 0 && currentResolutionIndex < resolutionOptions.Count)
        {
            Vector2Int res = resolutionOptions[currentResolutionIndex];
            Screen.SetResolution(res.x, res.y, savedFullscreen);
        }

        // Load menu background music
        AudioClip menuClip = Resources.Load<AudioClip>("Audio/menu_bgm");
        if (menuClip != null)
        {
            menuBgmSource = gameObject.AddComponent<AudioSource>();
            menuBgmSource.clip = menuClip;
            menuBgmSource.loop = true;
            menuBgmSource.volume = savedVolume;
            menuBgmSource.mute = (savedVolume <= 0.001f);
            menuBgmSource.playOnAwake = false;
            menuBgmSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM not found: Audio/menu_bgm");
        }

        // Load game background music (plays during gameplay)
        AudioClip gameClip = Resources.Load<AudioClip>("Audio/game_bgm");
        if (gameClip != null)
        {
            gameBgmSource = gameObject.AddComponent<AudioSource>();
            gameBgmSource.clip = gameClip;
            gameBgmSource.loop = true;
            gameBgmSource.volume = savedVolume;
            gameBgmSource.mute = (savedVolume <= 0.001f);
            gameBgmSource.playOnAwake = false;
        }
        else
        {
            Debug.LogWarning("BGM not found: Audio/game_bgm");
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

        // Soft radial gradient behind logo — makes title pop without visible border
        GameObject gradientObj = new GameObject("LogoGradient");
        gradientObj.transform.SetParent(menuPanel.transform, false);
        RectTransform gradientRect = gradientObj.AddComponent<RectTransform>();
        gradientRect.anchorMin = new Vector2(0.5f, 0.62f);
        gradientRect.anchorMax = new Vector2(0.5f, 0.62f);
        gradientRect.pivot = new Vector2(0.5f, 0.5f);
        gradientRect.anchoredPosition = Vector2.zero;
        gradientRect.sizeDelta = new Vector2(1050, 530);
        Image gradientImg = gradientObj.AddComponent<Image>();
        gradientImg.raycastTarget = false;
        gradientImg.sprite = CreateRadialGradientSprite(256, 128, 0.35f);
        gradientImg.type = Image.Type.Simple;
        gradientImg.preserveAspect = false;

        // Game logo image (calligraphy 水浒传 / 斗地主)
        GameObject logoObj = new GameObject("MenuLogo");
        logoObj.transform.SetParent(menuPanel.transform, false);
        RectTransform logoRect = logoObj.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.62f);
        logoRect.anchorMax = new Vector2(0.5f, 0.62f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.sizeDelta = new Vector2(750, 340);
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

            // Apply white-to-transparent shader so the white background disappears
            Shader whiteToTransparent = Shader.Find("UI/WhiteToTransparent");
            if (whiteToTransparent != null)
            {
                Material logoMat = new Material(whiteToTransparent);
                logoMat.SetFloat("_Threshold", 0.85f);
                logoMat.SetFloat("_Softness", 0.35f);
                logoImg.material = logoMat;
            }
        }
        else
        {
            Debug.LogWarning("Menu logo not found: Sprites/menu_logo");
        }

        // Fine outline to separate logo from busy background
        Outline logoOutline = logoObj.AddComponent<Outline>();
        logoOutline.effectColor = new Color(0.07f, 0.04f, 0.02f, 1f); // Deep carbon brown
        logoOutline.effectDistance = new Vector2(2f, -2f);

        // Soft shadow simulating ink wash diffusion ("墨晕" effect)
        Shadow logoShadow = logoObj.AddComponent<Shadow>();
        logoShadow.effectColor = new Color(0.07f, 0.04f, 0.02f, 0.6f); // Semi-transparent dark brown
        logoShadow.effectDistance = new Vector2(4f, -4f);

        // Start Game button — primary action
        Button startButton = CreateButton(menuPanel.transform, "StartButton",
            "\u5f00\u59cb\u6e38\u620f", Vector2.zero, new Vector2(280, 62));  // 开始游戏
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0.43f);
        startRect.anchorMax = new Vector2(0.5f, 0.43f);
        startRect.anchoredPosition = Vector2.zero;

        startButton.onClick.AddListener(() =>
        {
            menuPanel.SetActive(false);
            CreateGame();
        });

        // Settings button — between start and quit
        Button settingsButton = CreateButton(menuPanel.transform, "SettingsButton",
            "\u8bbe\u7f6e", Vector2.zero, new Vector2(280, 62));  // 设置
        RectTransform settingsRect = settingsButton.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(0.5f, 0.365f);
        settingsRect.anchorMax = new Vector2(0.5f, 0.365f);
        settingsRect.anchoredPosition = Vector2.zero;

        settingsButton.onClick.AddListener(() =>
        {
            if (settingsOverlay != null)
                settingsOverlay.SetActive(true);
        });

        // Quit Game button
        Button quitButton = CreateButton(menuPanel.transform, "QuitButton",
            "\u9000\u51fa\u6e38\u620f", Vector2.zero, new Vector2(280, 62));  // 退出游戏
        RectTransform quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0.30f);
        quitRect.anchorMax = new Vector2(0.5f, 0.30f);
        quitRect.anchoredPosition = Vector2.zero;

        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        // Build the settings panel (starts hidden)
        CreateSettingsPanel();
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

        // Switch from game BGM back to menu BGM
        if (gameBgmSource != null && gameBgmSource.isPlaying)
            gameBgmSource.Stop();
        if (menuBgmSource != null && !menuBgmSource.isPlaying)
            menuBgmSource.Play();
    }

    /// <summary>
    /// Creates all game objects, managers, and UI elements.
    /// </summary>
    private void CreateGame()
    {
        // Switch from menu BGM to game BGM
        if (menuBgmSource != null && menuBgmSource.isPlaying)
            menuBgmSource.Stop();
        if (gameBgmSource != null && !gameBgmSource.isPlaying)
            gameBgmSource.Play();

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

        // ==================== Player Info Cards ====================

        // Player 0 (Human / 宋江) - bottom left corner
        var (card0, infoText0, timerText0) = CreatePlayerInfoCard(gameObjectsRoot.transform,
            "PlayerCard_You", "Song_Jiang", "\u5b8b\u6c5f", 120f, vertical: true);
        RectTransform card0Rect = card0.GetComponent<RectTransform>();
        card0Rect.anchorMin = new Vector2(0, 0);
        card0Rect.anchorMax = new Vector2(0, 0);
        card0Rect.pivot = new Vector2(0, 0);
        card0Rect.anchoredPosition = new Vector2(15, 30);

        // Player 1 (AI Left / 林冲) - top left
        var (card1, infoText1, timerText1) = CreatePlayerInfoCard(gameObjectsRoot.transform,
            "PlayerCard_Left", "Lin_Chong", "\u6797\u51b2", 140f, vertical: false);
        RectTransform card1Rect = card1.GetComponent<RectTransform>();
        card1Rect.anchorMin = new Vector2(0, 1);
        card1Rect.anchorMax = new Vector2(0, 1);
        card1Rect.pivot = new Vector2(0, 1);
        card1Rect.anchoredPosition = new Vector2(15, -15);

        // Player 1 (AI Left) card backs area - below info card
        GameObject aiCards1 = new GameObject("AICards_Left");
        aiCards1.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform aiCards1Rect = aiCards1.AddComponent<RectTransform>();
        aiCards1Rect.anchorMin = new Vector2(0, 1);
        aiCards1Rect.anchorMax = new Vector2(0, 1);
        aiCards1Rect.pivot = new Vector2(0, 1);
        aiCards1Rect.anchoredPosition = new Vector2(15, -175);
        aiCards1Rect.sizeDelta = new Vector2(260, 50);

        // Player 2 (AI Right / 鲁智深) - top right
        var (card2, infoText2, timerText2) = CreatePlayerInfoCard(gameObjectsRoot.transform,
            "PlayerCard_Right", "Lu_Zhishen", "\u9c81\u667a\u6df1", 140f, vertical: false, mirrorHorizontal: true);
        RectTransform card2Rect = card2.GetComponent<RectTransform>();
        card2Rect.anchorMin = new Vector2(1, 1);
        card2Rect.anchorMax = new Vector2(1, 1);
        card2Rect.pivot = new Vector2(1, 1);
        card2Rect.anchoredPosition = new Vector2(-15, -15);

        // Player 2 (AI Right) card backs area - below info card
        GameObject aiCards2 = new GameObject("AICards_Right");
        aiCards2.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform aiCards2Rect = aiCards2.AddComponent<RectTransform>();
        aiCards2Rect.anchorMin = new Vector2(1, 1);
        aiCards2Rect.anchorMax = new Vector2(1, 1);
        aiCards2Rect.pivot = new Vector2(1, 1);
        aiCards2Rect.anchoredPosition = new Vector2(-15, -175);
        aiCards2Rect.sizeDelta = new Vector2(260, 50);

        TextMeshProUGUI[] playerInfoTexts = { infoText0, infoText1, infoText2 };
        TextMeshProUGUI[] timerTexts = { timerText0, timerText1, timerText2 };

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

        // Name labels above each played cards area (shows name + role during gameplay)
        // Each label has a small ink-style backing panel for readability

        GameObject labelBg0 = CreateLabelBacking(playedArea0.transform, "LabelBg_You",
            new Vector2(0.5f, 1), new Vector2(0, 18), new Vector2(160, 28));
        TextMeshProUGUI playedLabel0 = CreateText(labelBg0.transform, "PlayedLabel_You",
            "\u5b8b\u6c5f",  // 宋江
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(150, 24), 14);
        playedLabel0.color = new Color(0.95f, 0.85f, 0.55f); // Gold

        GameObject labelBg1 = CreateLabelBacking(playedArea1.transform, "LabelBg_Left",
            new Vector2(0.5f, 1), new Vector2(0, 18), new Vector2(160, 28));
        TextMeshProUGUI playedLabel1 = CreateText(labelBg1.transform, "PlayedLabel_Left",
            "\u6797\u51b2",  // 林冲
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(150, 24), 14);
        playedLabel1.color = new Color(0.95f, 0.85f, 0.55f); // Gold

        GameObject labelBg2 = CreateLabelBacking(playedArea2.transform, "LabelBg_Right",
            new Vector2(0.5f, 1), new Vector2(0, 18), new Vector2(160, 28));
        TextMeshProUGUI playedLabel2 = CreateText(labelBg2.transform, "PlayedLabel_Right",
            "\u9c81\u667a\u6df1",  // 鲁智深
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(150, 24), 14);
        playedLabel2.color = new Color(0.95f, 0.85f, 0.55f); // Gold

        TextMeshProUGUI[] playedAreaLabels = { playedLabel0, playedLabel1, playedLabel2 };

        // Hide name labels initially — they appear when cards are played
        labelBg0.SetActive(false);
        labelBg1.SetActive(false);
        labelBg2.SetActive(false);

        // ==================== Center Area ====================

        // Message text backing panel (top center) — dark ink wash style backdrop
        GameObject msgPanel = new GameObject("MessagePanel");
        msgPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform msgPanelRect = msgPanel.AddComponent<RectTransform>();
        msgPanelRect.anchorMin = new Vector2(0.5f, 0.95f);
        msgPanelRect.anchorMax = new Vector2(0.5f, 0.95f);
        msgPanelRect.pivot = new Vector2(0.5f, 0.5f);
        msgPanelRect.anchoredPosition = Vector2.zero;
        msgPanelRect.sizeDelta = new Vector2(650, 48);
        Image msgPanelBg = msgPanel.AddComponent<Image>();
        msgPanelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.8f);
        msgPanelBg.raycastTarget = false;
        Outline msgPanelOutline = msgPanel.AddComponent<Outline>();
        msgPanelOutline.effectColor = new Color(0.5f, 0.4f, 0.25f, 0.6f);
        msgPanelOutline.effectDistance = new Vector2(1, 1);

        // Message text (inside panel)
        TextMeshProUGUI messageText = CreateText(msgPanel.transform, "MessageText",
            "\u6b22\u8fce\u6765\u5230\u6c34\u6d52\u4f20\u6597\u5730\u4e3b\uff01",  // 欢迎来到水浒传斗地主！
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(620, 40), 20);
        messageText.color = new Color(0.95f, 0.85f, 0.55f); // Gold text

        // Combo type label backing panel (center, between played areas)
        GameObject comboPanel = new GameObject("ComboPanel");
        comboPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform comboPanelRect = comboPanel.AddComponent<RectTransform>();
        comboPanelRect.anchorMin = new Vector2(0.5f, 0.52f);
        comboPanelRect.anchorMax = new Vector2(0.5f, 0.52f);
        comboPanelRect.pivot = new Vector2(0.5f, 0.5f);
        comboPanelRect.anchoredPosition = Vector2.zero;
        comboPanelRect.sizeDelta = new Vector2(200, 36);
        Image comboPanelBg = comboPanel.AddComponent<Image>();
        comboPanelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.7f);
        comboPanelBg.raycastTarget = false;
        Outline comboPanelOutline = comboPanel.AddComponent<Outline>();
        comboPanelOutline.effectColor = new Color(0.5f, 0.4f, 0.25f, 0.5f);
        comboPanelOutline.effectDistance = new Vector2(1, 1);

        // Combo type text (inside panel)
        TextMeshProUGUI lastPlayedText = CreateText(comboPanel.transform, "LastPlayedText",
            "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(180, 30), 16);
        lastPlayedText.color = new Color(0.9f, 0.82f, 0.6f); // Warm gold

        // ==================== Multiplier Display (top center) ====================

        GameObject multiplierFrame = new GameObject("MultiplierFrame");
        multiplierFrame.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform multRect = multiplierFrame.AddComponent<RectTransform>();
        multRect.anchorMin = new Vector2(0.5f, 0.9f);
        multRect.anchorMax = new Vector2(0.5f, 0.9f);
        multRect.pivot = new Vector2(0.5f, 0.5f);
        multRect.anchoredPosition = Vector2.zero;
        multRect.sizeDelta = new Vector2(120, 50);

        // Dark background
        Image multBg = multiplierFrame.AddComponent<Image>();
        multBg.color = new Color(0.1f, 0.08f, 0.06f, 0.85f);
        multBg.raycastTarget = false;

        // Gold outline border
        Outline multOutline = multiplierFrame.AddComponent<Outline>();
        multOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        multOutline.effectDistance = new Vector2(2, 2);

        Outline multOutline2 = multiplierFrame.AddComponent<Outline>();
        multOutline2.effectColor = new Color(0.5f, 0.4f, 0.25f);
        multOutline2.effectDistance = new Vector2(-1, -1);

        // Multiplier text "×1"
        TextMeshProUGUI multiplierText = CreateText(multiplierFrame.transform, "MultiplierText",
            "\u00d71",  // ×1
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(100, 40), 28);
        multiplierText.color = new Color(0.95f, 0.85f, 0.55f); // Gold text

        // Start hidden, shown when bidding starts
        multiplierFrame.SetActive(false);

        // ==================== Bid Panel ====================

        GameObject bidPanel = new GameObject("BidPanel");
        bidPanel.transform.SetParent(gameObjectsRoot.transform, false);
        RectTransform bidPanelRect = bidPanel.AddComponent<RectTransform>();
        bidPanelRect.anchorMin = new Vector2(0.5f, 0);
        bidPanelRect.anchorMax = new Vector2(0.5f, 0);
        bidPanelRect.anchoredPosition = new Vector2(0, 260);
        bidPanelRect.sizeDelta = new Vector2(400, 50);

        // "叫地主" button (also used as "抢地主" in grabbing phase)
        Button callButton = CreateButton(bidPanel.transform, "CallButton",
            "\u53eb\u5730\u4e3b", new Vector2(-110, 0), new Vector2(160, 45));  // 叫地主
        // "不叫" button (also used as "不抢" in grabbing phase)
        Button noBidButton = CreateButton(bidPanel.transform, "NoBidButton",
            "\u4e0d\u53eb", new Vector2(110, 0), new Vector2(160, 45));  // 不叫

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
        pauseCenterRect.sizeDelta = new Vector2(350, 420);
        Image pauseBg = pauseCenter.AddComponent<Image>();
        pauseBg.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);

        // "PAUSED" title
        CreateText(pauseCenter.transform, "PausedTitle", "\u6682\u505c",  // 暂停
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -40), new Vector2(300, 50), 32);

        // Resume, Settings, Restart, Return to Menu, Quit buttons
        Button resumeButton = CreateButton(pauseCenter.transform, "ResumeButton",
            "\u7ee7\u7eed\u6e38\u620f", new Vector2(0, 80), new Vector2(220, 50));  // 继续游戏
        Button pauseSettingsButton = CreateButton(pauseCenter.transform, "PauseSettingsButton",
            "\u8bbe\u7f6e", new Vector2(0, 20), new Vector2(220, 50));  // 设置
        Button pauseRestartButton = CreateButton(pauseCenter.transform, "PauseRestartButton",
            "\u91cd\u65b0\u5f00\u59cb", new Vector2(0, -40), new Vector2(220, 50));  // 重新开始
        Button mainMenuButton = CreateButton(pauseCenter.transform, "MainMenuButton",
            "\u8fd4\u56de\u4e3b\u83dc\u5355", new Vector2(0, -100), new Vector2(220, 50));  // 返回主菜单
        Button quitButton = CreateButton(pauseCenter.transform, "QuitButton",
            "\u9000\u51fa\u6e38\u620f", new Vector2(0, -160), new Vector2(220, 50));  // 退出游戏

        // ---- In-game settings sub-panel (shown over pause menu) ----
        GameObject gameSettingsPanel = CreatePanel(pauseOverlay.transform, "GameSettingsPanel",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.5f));

        GameObject gsCenter = new GameObject("GameSettingsCenter");
        gsCenter.transform.SetParent(gameSettingsPanel.transform, false);
        RectTransform gsCenterRect = gsCenter.AddComponent<RectTransform>();
        gsCenterRect.anchorMin = new Vector2(0.5f, 0.5f);
        gsCenterRect.anchorMax = new Vector2(0.5f, 0.5f);
        gsCenterRect.sizeDelta = new Vector2(500, 450);
        Image gsCenterBg = gsCenter.AddComponent<Image>();
        gsCenterBg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
        Outline gsOutline = gsCenter.AddComponent<Outline>();
        gsOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        gsOutline.effectDistance = new Vector2(2, 2);

        // Title
        CreateText(gsCenter.transform, "GSTitle", "\u8bbe\u7f6e",  // 设置
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -35), new Vector2(300, 50), 30);

        // ---- BGM Volume ----
        CreateText(gsCenter.transform, "GSBgmLabel", "\u80cc\u666f\u97f3\u4e50",  // 背景音乐
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, 100), new Vector2(140, 30), 18);

        float curBgmVol = gameBgmSource != null ? gameBgmSource.volume : 0.3f;
        TextMeshProUGUI gsBgmValue = CreateText(gsCenter.transform, "GSBgmValue",
            Mathf.RoundToInt(curBgmVol * 100) + "%",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-45, 100), new Vector2(60, 30), 18);

        Slider gsBgmSlider = CreateSlider(gsCenter.transform, "GSBgmSlider",
            new Vector2(0, 100), new Vector2(220, 20), curBgmVol);

        gsBgmSlider.onValueChanged.AddListener((float value) =>
        {
            SetBgmVolume(value);
            gsBgmValue.text = Mathf.RoundToInt(value * 100) + "%";
            PlayerPrefs.SetFloat("MusicVolume", value);
        });

        // ---- SFX Volume ----
        CreateText(gsCenter.transform, "GSSfxLabel", "\u97f3\u6548",  // 音效
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, 40), new Vector2(140, 30), 18);

        float curSfxVol = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 0.5f;
        TextMeshProUGUI gsSfxValue = CreateText(gsCenter.transform, "GSSfxValue",
            Mathf.RoundToInt(curSfxVol * 100) + "%",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-45, 40), new Vector2(60, 30), 18);

        Slider gsSfxSlider = CreateSlider(gsCenter.transform, "GSSfxSlider",
            new Vector2(0, 40), new Vector2(220, 20), curSfxVol);

        gsSfxSlider.onValueChanged.AddListener((float value) =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.SetVolume(value);
            gsSfxValue.text = Mathf.RoundToInt(value * 100) + "%";
        });

        // ---- Resolution ----
        CreateText(gsCenter.transform, "GSResLabel", "\u5206\u8fa8\u7387",  // 分辨率
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, -30), new Vector2(140, 30), 18);

        Vector2Int curRes = resolutionOptions[currentResolutionIndex];
        TextMeshProUGUI gsResValue = CreateText(gsCenter.transform, "GSResValue",
            curRes.x + "x" + curRes.y,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(30, -30), new Vector2(160, 30), 18);

        Button gsResLeft = CreateButton(gsCenter.transform, "GSResLeft",
            "<", new Vector2(110, -30), new Vector2(40, 35));
        gsResLeft.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;

        Button gsResRight = CreateButton(gsCenter.transform, "GSResRight",
            ">", new Vector2(280, -30), new Vector2(40, 35));
        gsResRight.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;

        gsResLeft.onClick.AddListener(() =>
        {
            currentResolutionIndex = (currentResolutionIndex - 1 + resolutionOptions.Count) % resolutionOptions.Count;
            ApplyResolution(gsResValue);
        });

        gsResRight.onClick.AddListener(() =>
        {
            currentResolutionIndex = (currentResolutionIndex + 1) % resolutionOptions.Count;
            ApplyResolution(gsResValue);
        });

        // ---- Fullscreen toggle ----
        CreateText(gsCenter.transform, "GSFullLabel", "\u5168\u5c4f\u6a21\u5f0f",  // 全屏模式
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, -90), new Vector2(140, 30), 18);

        Button gsFullBtn = CreateButton(gsCenter.transform, "GSFullToggle",
            Screen.fullScreen ? "\u5f00" : "\u5173",  // 开 or 关
            new Vector2(195, -90), new Vector2(80, 35));
        TextMeshProUGUI gsFullText = gsFullBtn.GetComponentInChildren<TextMeshProUGUI>();

        gsFullBtn.onClick.AddListener(() =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            gsFullText.text = Screen.fullScreen ? "\u5f00" : "\u5173";
            PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        });

        // ---- Back button ----
        Button gsBackBtn = CreateButton(gsCenter.transform, "GSBackBtn",
            "\u8fd4\u56de", new Vector2(0, -160), new Vector2(180, 50));  // 返回
        RectTransform gsBackRect = gsBackBtn.GetComponent<RectTransform>();
        gsBackRect.anchorMin = new Vector2(0.5f, 0.5f);
        gsBackRect.anchorMax = new Vector2(0.5f, 0.5f);

        gsBackBtn.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            gameSettingsPanel.SetActive(false);
        });

        // Start hidden
        gameSettingsPanel.SetActive(false);

        // Wire settings button to show the sub-panel
        pauseSettingsButton.onClick.AddListener(() =>
        {
            gameSettingsPanel.SetActive(true);
        });

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
            callButton, noBidButton,
            bidPanel, playPanel,
            messageText, lastPlayedText,
            playerInfoTexts, restartButton,
            playedAreas, aiCardAreas,
            playedAreaLabels, timerTexts,
            multiplierText, multiplierFrame
        );

        // Wire pause panel
        uiManager.SetPausePanel(pauseOverlay, resumeButton, pauseRestartButton, quitButton);

        // Wire "return to main menu" button - calls ReturnToMainMenu on this GameSetup
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Start the game
        uiManager.BeginGame();
    }

    // ==================== Settings Panel ====================

    /// <summary>
    /// Creates the settings panel overlay with volume, resolution, and fullscreen controls.
    /// Panel is parented to menuPanel and starts hidden.
    /// </summary>
    private void CreateSettingsPanel()
    {
        // Full-screen semi-transparent dark overlay
        settingsOverlay = CreatePanel(menuPanel.transform, "SettingsOverlay",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.7f));

        // Center panel background
        GameObject centerPanel = new GameObject("SettingsCenter");
        centerPanel.transform.SetParent(settingsOverlay.transform, false);
        RectTransform centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(500, 400);
        Image centerBg = centerPanel.AddComponent<Image>();
        centerBg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

        // Gold border outline
        Outline panelOutline = centerPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        panelOutline.effectDistance = new Vector2(2, 2);

        // Title: 设置
        CreateText(centerPanel.transform, "SettingsTitle", "\u8bbe\u7f6e",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -35), new Vector2(300, 50), 30);

        // ---- Volume control ----
        // Label
        CreateText(centerPanel.transform, "VolumeLabel", "\u97f3\u4e50\u97f3\u91cf",  // 音乐音量
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, 80), new Vector2(140, 30), 18);

        // Volume percentage text (updated by slider)
        float currentBgmVol = menuBgmSource != null ? menuBgmSource.volume : 0.3f;
        TextMeshProUGUI volumeValueText = CreateText(centerPanel.transform, "VolumeValue",
            Mathf.RoundToInt(currentBgmVol * 100) + "%",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-45, 80), new Vector2(60, 30), 18);

        // BGM volume slider — controls both menu and game BGM
        Slider volumeSlider = CreateSlider(centerPanel.transform, "VolumeSlider",
            new Vector2(0, 80), new Vector2(220, 20), currentBgmVol);

        volumeSlider.onValueChanged.AddListener((float value) =>
        {
            SetBgmVolume(value);
            volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
            PlayerPrefs.SetFloat("MusicVolume", value);
        });

        // ---- Resolution control ----
        // Label
        CreateText(centerPanel.transform, "ResolutionLabel", "\u5206\u8fa8\u7387",  // 分辨率
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, 10), new Vector2(140, 30), 18);

        // Resolution value text
        Vector2Int currentRes = resolutionOptions[currentResolutionIndex];
        TextMeshProUGUI resolutionValueText = CreateText(centerPanel.transform, "ResolutionValue",
            currentRes.x + "x" + currentRes.y,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(30, 10), new Vector2(160, 30), 18);

        // Left arrow button "<"
        Button resLeftBtn = CreateButton(centerPanel.transform, "ResLeftBtn",
            "<", new Vector2(110, 10), new Vector2(40, 35));
        // Smaller font for arrow
        resLeftBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;

        // Right arrow button ">"
        Button resRightBtn = CreateButton(centerPanel.transform, "ResRightBtn",
            ">", new Vector2(280, 10), new Vector2(40, 35));
        resRightBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;

        resLeftBtn.onClick.AddListener(() =>
        {
            currentResolutionIndex = (currentResolutionIndex - 1 + resolutionOptions.Count) % resolutionOptions.Count;
            ApplyResolution(resolutionValueText);
        });

        resRightBtn.onClick.AddListener(() =>
        {
            currentResolutionIndex = (currentResolutionIndex + 1) % resolutionOptions.Count;
            ApplyResolution(resolutionValueText);
        });

        // ---- Fullscreen toggle ----
        // Label
        CreateText(centerPanel.transform, "FullscreenLabel", "\u5168\u5c4f\u6a21\u5f0f",  // 全屏模式
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, -60), new Vector2(140, 30), 18);

        // Toggle button showing 开/关
        Button fullscreenBtn = CreateButton(centerPanel.transform, "FullscreenToggle",
            Screen.fullScreen ? "\u5f00" : "\u5173",  // 开 or 关
            new Vector2(195, -60), new Vector2(80, 35));
        TextMeshProUGUI fullscreenText = fullscreenBtn.GetComponentInChildren<TextMeshProUGUI>();

        fullscreenBtn.onClick.AddListener(() =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            fullscreenText.text = Screen.fullScreen ? "\u5f00" : "\u5173";
            PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        });

        // ---- Back button ----
        Button backBtn = CreateButton(centerPanel.transform, "SettingsBackBtn",
            "\u8fd4\u56de", new Vector2(0, -145), new Vector2(180, 50));  // 返回
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);

        backBtn.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            settingsOverlay.SetActive(false);
        });

        // Start hidden
        settingsOverlay.SetActive(false);
    }

    /// <summary>
    /// Applies the currently selected resolution and saves the setting.
    /// </summary>
    private void ApplyResolution(TextMeshProUGUI resText)
    {
        Vector2Int res = resolutionOptions[currentResolutionIndex];
        resText.text = res.x + "x" + res.y;
        Screen.SetResolution(res.x, res.y, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
    }

    /// <summary>
    /// Sets volume on both menu and game BGM sources and handles muting.
    /// </summary>
    private void SetBgmVolume(float value)
    {
        if (menuBgmSource != null)
        {
            menuBgmSource.volume = value;
            menuBgmSource.mute = (value <= 0.001f);
        }
        if (gameBgmSource != null)
        {
            gameBgmSource.volume = value;
            gameBgmSource.mute = (value <= 0.001f);
        }
    }

    /// <summary>
    /// Creates a horizontal slider UI element using Unity's built-in Slider component.
    /// </summary>
    private Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size, float initialValue)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = size;

        // Background bar (dark track)
        GameObject bgBar = new GameObject("Background");
        bgBar.transform.SetParent(sliderObj.transform, false);
        RectTransform bgBarRect = bgBar.AddComponent<RectTransform>();
        bgBarRect.anchorMin = Vector2.zero;
        bgBarRect.anchorMax = Vector2.one;
        bgBarRect.offsetMin = Vector2.zero;
        bgBarRect.offsetMax = Vector2.zero;
        Image bgBarImg = bgBar.AddComponent<Image>();
        bgBarImg.color = new Color(0.2f, 0.18f, 0.15f, 1f);

        // Fill area container
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        // Fill bar (gold, shows current value)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.7f, 0.35f, 1f); // Gold fill

        // Handle slide area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, -5);
        handleAreaRect.offsetMax = new Vector2(-10, 5);

        // Handle (draggable knob)
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 30);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.85f, 0.55f, 1f); // Gold handle

        // Wire up the Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.targetGraphic = handleImg;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;

        return slider;
    }

    // ==================== UI Factory Methods ====================

    /// <summary>
    /// Creates a small dark backing panel for text labels floating over the background.
    /// Provides contrast so text is readable on busy ink wash backgrounds.
    /// </summary>
    private GameObject CreateLabelBacking(Transform parent, string name,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.04f, 0.7f);
        bg.raycastTarget = false;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.4f, 0.25f, 0.4f);
        outline.effectDistance = new Vector2(1, 1);

        return obj;
    }

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
    /// Generates a radial gradient texture at runtime — black center fading to transparent edges.
    /// Used as a subtle backdrop to lift UI elements from busy backgrounds.
    /// </summary>
    private Sprite CreateRadialGradientSprite(int width, int height, float peakAlpha)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalized distance from center (0 = center, 1 = edge)
                float dx = (x - width * 0.5f) / (width * 0.5f);
                float dy = (y - height * 0.5f) / (height * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Smooth falloff: fully opaque at center, transparent at edge
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0f, 1f, dist)) * peakAlpha;
                pixels[y * width + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Creates a unified player info card with avatar, name, card count, and timer.
    /// Returns the info text and timer text so they can be updated during gameplay.
    ///
    /// For horizontal layout (AI players): avatar on one side, name + card count + timer stacked.
    /// For vertical layout (human player): large avatar on top, name + info + timer below.
    /// </summary>
    private (GameObject card, TextMeshProUGUI infoText, TextMeshProUGUI timerText) CreatePlayerInfoCard(
        Transform parent, string name, string spriteName, string displayName,
        float avatarSize, bool vertical, bool mirrorHorizontal = false)
    {
        // Calculate panel size based on layout
        float textAreaWidth = 120f; // Wider text area for larger cards
        float panelWidth, panelHeight;
        if (vertical)
        {
            panelWidth = avatarSize + 20;   // Avatar width + padding
            panelHeight = avatarSize + 65;  // Avatar + name + info + timer below
        }
        else
        {
            panelWidth = avatarSize + textAreaWidth + 20; // Avatar + text area + padding
            panelHeight = avatarSize + 16;  // Avatar height + padding
        }

        // Main card panel with dark semi-transparent background
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.06f, 0.04f, 0.85f);
        cardBg.raycastTarget = false;

        // Gold border on the card panel
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        cardOutline.effectDistance = new Vector2(2, 2);

        // Second outline pass for thicker gold border
        Outline cardOutline2 = card.AddComponent<Outline>();
        cardOutline2.effectColor = new Color(0.5f, 0.4f, 0.25f);
        cardOutline2.effectDistance = new Vector2(-1, -1);

        // --- Avatar portrait ---
        GameObject portrait = new GameObject("Portrait");
        portrait.transform.SetParent(card.transform, false);
        RectTransform portraitRect = portrait.AddComponent<RectTransform>();
        portraitRect.sizeDelta = new Vector2(avatarSize, avatarSize);
        Image portraitImg = portrait.AddComponent<Image>();
        portraitImg.raycastTarget = false;

        // Load hero texture
        Texture2D heroTex = Resources.Load<Texture2D>("Sprites/" + spriteName);
        if (heroTex != null)
        {
            portraitImg.sprite = Sprite.Create(heroTex,
                new Rect(0, 0, heroTex.width, heroTex.height),
                new Vector2(0.5f, 0.5f));
            portraitImg.type = Image.Type.Simple;
            portraitImg.preserveAspect = true;
        }
        else
        {
            portraitImg.color = new Color(0.3f, 0.25f, 0.2f);
            Debug.LogWarning("Hero avatar not found: Sprites/" + spriteName);
        }

        // Gold border on the portrait
        Outline portraitOutline = portrait.AddComponent<Outline>();
        portraitOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
        portraitOutline.effectDistance = new Vector2(2, 2);

        Outline portraitOutline2 = portrait.AddComponent<Outline>();
        portraitOutline2.effectColor = new Color(0.6f, 0.5f, 0.3f);
        portraitOutline2.effectDistance = new Vector2(-2, -2);

        // --- Name label ---
        GameObject nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        if (chineseFont != null) nameText.font = chineseFont;
        nameText.text = displayName;
        nameText.color = new Color(0.95f, 0.85f, 0.55f); // Gold name
        nameText.alignment = TextAlignmentOptions.Center;

        // --- Card count / info label ---
        GameObject infoObj = new GameObject("InfoLabel");
        infoObj.transform.SetParent(card.transform, false);
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
        if (chineseFont != null) infoText.font = chineseFont;
        infoText.text = "\u624b\u724c: 17";  // 手牌: 17
        infoText.color = new Color(0.8f, 0.75f, 0.65f); // Warm light text
        infoText.alignment = TextAlignmentOptions.Center;

        // --- Timer label (hidden by default, shown when it's this player's turn) ---
        GameObject timerObj = new GameObject("TimerLabel");
        timerObj.transform.SetParent(card.transform, false);
        RectTransform timerRect = timerObj.AddComponent<RectTransform>();
        TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
        if (chineseFont != null) timerText.font = chineseFont;
        timerText.text = "30";
        timerText.color = new Color(1f, 0.4f, 0.3f); // Red-orange for urgency
        timerText.alignment = TextAlignmentOptions.Center;
        timerObj.SetActive(false); // Hidden until player's turn

        // --- Position elements based on layout ---
        if (vertical)
        {
            // Vertical: avatar centered on top, name + info + timer stacked below
            portraitRect.anchorMin = new Vector2(0.5f, 1);
            portraitRect.anchorMax = new Vector2(0.5f, 1);
            portraitRect.pivot = new Vector2(0.5f, 1);
            portraitRect.anchoredPosition = new Vector2(0, -6);

            nameText.fontSize = 16;
            nameRect.anchorMin = new Vector2(0.5f, 0);
            nameRect.anchorMax = new Vector2(0.5f, 0);
            nameRect.pivot = new Vector2(0.5f, 0);
            nameRect.anchoredPosition = new Vector2(0, 32);
            nameRect.sizeDelta = new Vector2(panelWidth, 24);

            infoText.fontSize = 13;
            infoRect.anchorMin = new Vector2(0.5f, 0);
            infoRect.anchorMax = new Vector2(0.5f, 0);
            infoRect.pivot = new Vector2(0.5f, 0);
            infoRect.anchoredPosition = new Vector2(0, 14);
            infoRect.sizeDelta = new Vector2(panelWidth, 20);

            timerText.fontSize = 18;
            timerRect.anchorMin = new Vector2(0.5f, 0);
            timerRect.anchorMax = new Vector2(0.5f, 0);
            timerRect.pivot = new Vector2(0.5f, 1);
            timerRect.anchoredPosition = new Vector2(0, -2);
            timerRect.sizeDelta = new Vector2(panelWidth, 24);
        }
        else if (mirrorHorizontal)
        {
            // Horizontal mirrored: text on left, avatar on right
            portraitRect.anchorMin = new Vector2(1, 0.5f);
            portraitRect.anchorMax = new Vector2(1, 0.5f);
            portraitRect.pivot = new Vector2(1, 0.5f);
            portraitRect.anchoredPosition = new Vector2(-6, 0);

            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Right;
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(8, 20);
            nameRect.sizeDelta = new Vector2(textAreaWidth, 26);

            infoText.fontSize = 14;
            infoText.alignment = TextAlignmentOptions.Right;
            infoRect.anchorMin = new Vector2(0, 0.5f);
            infoRect.anchorMax = new Vector2(0, 0.5f);
            infoRect.pivot = new Vector2(0, 0.5f);
            infoRect.anchoredPosition = new Vector2(8, -4);
            infoRect.sizeDelta = new Vector2(textAreaWidth, 22);

            timerText.fontSize = 20;
            timerText.alignment = TextAlignmentOptions.Right;
            timerRect.anchorMin = new Vector2(0, 0.5f);
            timerRect.anchorMax = new Vector2(0, 0.5f);
            timerRect.pivot = new Vector2(0, 0.5f);
            timerRect.anchoredPosition = new Vector2(8, -28);
            timerRect.sizeDelta = new Vector2(textAreaWidth, 26);
        }
        else
        {
            // Horizontal normal: avatar on left, text on right
            portraitRect.anchorMin = new Vector2(0, 0.5f);
            portraitRect.anchorMax = new Vector2(0, 0.5f);
            portraitRect.pivot = new Vector2(0, 0.5f);
            portraitRect.anchoredPosition = new Vector2(6, 0);

            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Left;
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(avatarSize + 14, 20);
            nameRect.sizeDelta = new Vector2(textAreaWidth, 26);

            infoText.fontSize = 14;
            infoText.alignment = TextAlignmentOptions.Left;
            infoRect.anchorMin = new Vector2(0, 0.5f);
            infoRect.anchorMax = new Vector2(0, 0.5f);
            infoRect.pivot = new Vector2(0, 0.5f);
            infoRect.anchoredPosition = new Vector2(avatarSize + 14, -4);
            infoRect.sizeDelta = new Vector2(textAreaWidth, 22);

            timerText.fontSize = 20;
            timerText.alignment = TextAlignmentOptions.Left;
            timerRect.anchorMin = new Vector2(0, 0.5f);
            timerRect.anchorMax = new Vector2(0, 0.5f);
            timerRect.pivot = new Vector2(0, 0.5f);
            timerRect.anchoredPosition = new Vector2(avatarSize + 14, -28);
            timerRect.sizeDelta = new Vector2(textAreaWidth, 26);
        }

        return (card, infoText, timerText);
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
        if (buttonSprite != null)
        {
            // Use themed wooden plaque background
            img.sprite = buttonSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;

            // Apply shader to remove fake checkerboard background
            Shader whiteToTransparent = Shader.Find("UI/WhiteToTransparent");
            if (whiteToTransparent != null)
            {
                Material btnMat = new Material(whiteToTransparent);
                btnMat.SetFloat("_Threshold", 0.75f);
                btnMat.SetFloat("_Softness", 0.25f);
                img.material = btnMat;
            }
        }
        else
        {
            img.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
        }

        // Diagonal ink wash glow behind the wooden plaque
        Shadow btnShadow = obj.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0.05f, 0.03f, 0.01f, 0.5f);
        btnShadow.effectDistance = new Vector2(3f, -3f);

        // Vertical contact shadow — makes the plaque feel "resting" on the scene
        Shadow contactShadow = obj.AddComponent<Shadow>();
        contactShadow.effectColor = new Color(0.02f, 0.01f, 0f, 0.7f);
        contactShadow.effectDistance = new Vector2(0f, -4f);

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.75f, 1f);    // Warm glow on hover
        colors.pressedColor = new Color(0.7f, 0.65f, 0.55f, 1f);      // Darker when pressed
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
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
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.85f, 0.55f); // Gold text

        // Text outline for readability on the wooden plaque
        Outline textOutline = textObj.AddComponent<Outline>();
        textOutline.effectColor = new Color(0.07f, 0.04f, 0.02f, 1f);
        textOutline.effectDistance = new Vector2(1f, -1f);

        return btn;
    }
}
