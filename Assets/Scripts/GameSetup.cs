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

    private void Start()
    {
        CreateGame();
    }

    /// <summary>
    /// Creates all game objects, managers, and UI elements.
    /// </summary>
    private void CreateGame()
    {
        // ==================== Load Chinese Font ====================

        // Create TMP font dynamically from .ttf at runtime (supports full character set)
        Font notoFont = Resources.Load<Font>("NotoSansSC-Regular");
        if (notoFont != null)
        {
            chineseFont = TMP_FontAsset.CreateFontAsset(notoFont);
        }
        else
        {
            Debug.LogWarning("NotoSansSC-Regular.ttf not found in Resources. Chinese text will not display.");
        }

        // ==================== Managers ====================

        // Create a central manager object with all game flow components
        GameObject managerObj = new GameObject("GameManagers");
        GameManager gameManager = managerObj.AddComponent<GameManager>();
        TurnManager turnManager = managerObj.AddComponent<TurnManager>();
        BidManager bidManager = managerObj.AddComponent<BidManager>();
        ScoreManager scoreManager = managerObj.AddComponent<ScoreManager>();
        AIPlayer aiPlayer = managerObj.AddComponent<AIPlayer>();

        // ==================== Canvas ====================

        // Create the main UI canvas
        GameObject canvasObj = new GameObject("Canvas");
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

        // ==================== Background ====================

        // Load Water Margin ink wash background image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
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
        handArea.transform.SetParent(canvasObj.transform, false);
        RectTransform handRect = handArea.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0);
        handRect.anchorMax = new Vector2(0.5f, 0);
        handRect.pivot = new Vector2(0.5f, 0);
        handRect.anchoredPosition = new Vector2(0, 15);
        handRect.sizeDelta = new Vector2(1600, 180);
        HandView handView = handArea.AddComponent<HandView>();

        // ==================== Player Info Labels ====================

        // Player 0 (Human) - bottom center, below hand area
        TextMeshProUGUI playerInfo0 = CreateText(canvasObj.transform, "PlayerInfo_You",
            "\u4f60",  // 你
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 200), new Vector2(300, 30), 14);

        // Player 1 (AI Left) - top left corner
        TextMeshProUGUI playerInfo1 = CreateText(canvasObj.transform, "PlayerInfo_Left",
            "\u6797\u51b2 | \u624b\u724c: 17",  // 林冲 | 手牌: 17
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(120, -30), new Vector2(220, 30), 16);

        // Player 2 (AI Right) - top right corner
        TextMeshProUGUI playerInfo2 = CreateText(canvasObj.transform, "PlayerInfo_Right",
            "\u9c81\u667a\u6df1 | \u624b\u724c: 17",  // 鲁智深 | 手牌: 17
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-130, -30), new Vector2(240, 30), 16);

        TextMeshProUGUI[] playerInfoTexts = { playerInfo0, playerInfo1, playerInfo2 };

        // ==================== Played Cards Areas (inverted triangle layout) ====================

        // Player 0 (Human) played cards - center, below AI played areas
        GameObject playedArea0 = new GameObject("PlayedCards_You");
        playedArea0.transform.SetParent(canvasObj.transform, false);
        RectTransform played0Rect = playedArea0.AddComponent<RectTransform>();
        played0Rect.anchorMin = new Vector2(0.5f, 0.38f);
        played0Rect.anchorMax = new Vector2(0.5f, 0.38f);
        played0Rect.pivot = new Vector2(0.5f, 0.5f);
        played0Rect.anchoredPosition = Vector2.zero;
        played0Rect.sizeDelta = new Vector2(900, 160);

        // Player 1 (AI Left) played cards - center-left, below Lin Chong info
        GameObject playedArea1 = new GameObject("PlayedCards_Left");
        playedArea1.transform.SetParent(canvasObj.transform, false);
        RectTransform played1Rect = playedArea1.AddComponent<RectTransform>();
        played1Rect.anchorMin = new Vector2(0.3f, 0.65f);
        played1Rect.anchorMax = new Vector2(0.3f, 0.65f);
        played1Rect.pivot = new Vector2(0.5f, 0.5f);
        played1Rect.anchoredPosition = Vector2.zero;
        played1Rect.sizeDelta = new Vector2(500, 150);

        // Player 2 (AI Right) played cards - center-right, below Lu Zhishen info
        GameObject playedArea2 = new GameObject("PlayedCards_Right");
        playedArea2.transform.SetParent(canvasObj.transform, false);
        RectTransform played2Rect = playedArea2.AddComponent<RectTransform>();
        played2Rect.anchorMin = new Vector2(0.7f, 0.65f);
        played2Rect.anchorMax = new Vector2(0.7f, 0.65f);
        played2Rect.pivot = new Vector2(0.5f, 0.5f);
        played2Rect.anchoredPosition = Vector2.zero;
        played2Rect.sizeDelta = new Vector2(500, 150);

        Transform[] playedAreas = { playedArea0.transform, playedArea1.transform, playedArea2.transform };

        // ==================== Center Area ====================

        // Message text (top center)
        TextMeshProUGUI messageText = CreateText(canvasObj.transform, "MessageText",
            "\u6b22\u8fce\u6765\u5230\u6c34\u6d52\u4f20\u6597\u5730\u4e3b\uff01",  // 欢迎来到水浒传斗地主！
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f),
            Vector2.zero, new Vector2(600, 40), 20);

        // Combo type label (center, between played areas)
        TextMeshProUGUI lastPlayedText = CreateText(canvasObj.transform, "LastPlayedText",
            "",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, new Vector2(300, 30), 16);

        // ==================== Bid Panel ====================

        GameObject bidPanel = new GameObject("BidPanel");
        bidPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform bidPanelRect = bidPanel.AddComponent<RectTransform>();
        bidPanelRect.anchorMin = new Vector2(0.5f, 0);
        bidPanelRect.anchorMax = new Vector2(0.5f, 0);
        bidPanelRect.anchoredPosition = new Vector2(0, 240);
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
        playPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform playPanelRect = playPanel.AddComponent<RectTransform>();
        playPanelRect.anchorMin = new Vector2(0.5f, 0);
        playPanelRect.anchorMax = new Vector2(0.5f, 0);
        playPanelRect.anchoredPosition = new Vector2(0, 240);
        playPanelRect.sizeDelta = new Vector2(400, 50);

        Button playButton = CreateButton(playPanel.transform, "PlayButton",
            "\u51fa\u724c", new Vector2(-110, 0), new Vector2(160, 45));  // 出牌
        Button passButton = CreateButton(playPanel.transform, "PassButton",
            "\u4e0d\u51fa", new Vector2(110, 0), new Vector2(160, 45));  // 不出

        // ==================== Restart Button ====================

        Button restartButton = CreateButton(canvasObj.transform, "RestartButton",
            "\u518d\u6765\u4e00\u5c40", Vector2.zero, new Vector2(200, 50));  // 再来一局
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = Vector2.zero;

        // ==================== Side Panels ====================

        // Skill panel (right side) - placeholder frame
        GameObject skillPanel = new GameObject("SkillPanel");
        skillPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform skillRect = skillPanel.AddComponent<RectTransform>();
        skillRect.anchorMin = new Vector2(1, 0);
        skillRect.anchorMax = new Vector2(1, 0);
        skillRect.pivot = new Vector2(1, 0);
        skillRect.anchoredPosition = new Vector2(-15, 15);
        skillRect.sizeDelta = new Vector2(150, 200);
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
        GameObject pauseOverlay = CreatePanel(canvasObj.transform, "PauseOverlay",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.7f));

        // Center panel background
        GameObject pauseCenter = new GameObject("PauseCenter");
        pauseCenter.transform.SetParent(pauseOverlay.transform, false);
        RectTransform pauseCenterRect = pauseCenter.AddComponent<RectTransform>();
        pauseCenterRect.anchorMin = new Vector2(0.5f, 0.5f);
        pauseCenterRect.anchorMax = new Vector2(0.5f, 0.5f);
        pauseCenterRect.sizeDelta = new Vector2(350, 300);
        Image pauseBg = pauseCenter.AddComponent<Image>();
        pauseBg.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);

        // "PAUSED" title
        CreateText(pauseCenter.transform, "PausedTitle", "\u6682\u505c",  // 暂停
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -40), new Vector2(300, 50), 32);

        // Resume, Restart, Quit buttons
        Button resumeButton = CreateButton(pauseCenter.transform, "ResumeButton",
            "\u7ee7\u7eed\u6e38\u620f", new Vector2(0, 30), new Vector2(220, 50));  // 继续游戏
        Button pauseRestartButton = CreateButton(pauseCenter.transform, "PauseRestartButton",
            "\u91cd\u65b0\u5f00\u59cb", new Vector2(0, -30), new Vector2(220, 50));  // 重新开始
        Button quitButton = CreateButton(pauseCenter.transform, "QuitButton",
            "\u9000\u51fa\u6e38\u620f", new Vector2(0, -90), new Vector2(220, 50));  // 退出游戏

        // Start hidden
        pauseOverlay.SetActive(false);

        // ==================== Wire Everything Up ====================

        aiPlayer.Init(turnManager, bidManager);
        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();
        handView.Init(uiManager);
        uiManager.Init(handView, turnManager, bidManager, scoreManager, aiPlayer);
        uiManager.SetUIElements(
            playButton, passButton,
            bid1Button, bid2Button, bid3Button, noBidButton,
            bidPanel, playPanel,
            messageText, lastPlayedText,
            playerInfoTexts, restartButton,
            playedAreas
        );

        // Wire pause panel
        uiManager.SetPausePanel(pauseOverlay, resumeButton, pauseRestartButton, quitButton);

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
        img.color = new Color(0.9f, 0.85f, 0.7f); // Light tan button color

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.pressedColor = new Color(0.7f, 0.65f, 0.5f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
        btn.colors = colors;

        // Button label text
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
        tmp.color = new Color(0.2f, 0.15f, 0.1f); // Dark brown text

        return btn;
    }
}
