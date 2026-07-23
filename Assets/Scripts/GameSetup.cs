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
    private void Start()
    {
        CreateGame();
    }

    /// <summary>
    /// Creates all game objects, managers, and UI elements.
    /// </summary>
    private void CreateGame()
    {
        // ==================== Managers ====================

        // Create a central manager object with all game flow components
        GameObject managerObj = new GameObject("GameManagers");
        GameManager gameManager = managerObj.AddComponent<GameManager>();
        TurnManager turnManager = managerObj.AddComponent<TurnManager>();
        BidManager bidManager = managerObj.AddComponent<BidManager>();
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
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ==================== Background ====================

        GameObject bgObj = CreatePanel(canvasObj.transform, "Background",
            Vector2.zero, Vector2.one, new Color(0.1f, 0.3f, 0.15f)); // Dark green table

        // ==================== Player Hand Area (Bottom) ====================

        GameObject handArea = new GameObject("HandArea");
        handArea.transform.SetParent(canvasObj.transform, false);
        RectTransform handRect = handArea.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0);
        handRect.anchorMax = new Vector2(0.5f, 0);
        handRect.pivot = new Vector2(0.5f, 0);
        handRect.anchoredPosition = new Vector2(0, 30);
        handRect.sizeDelta = new Vector2(1200, 150);
        HandView handView = handArea.AddComponent<HandView>();

        // ==================== Player Info Labels ====================

        // Player 0 (Human) - bottom center
        TextMeshProUGUI playerInfo0 = CreateText(canvasObj.transform, "PlayerInfo_You",
            "You\nCards: 17",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 185), new Vector2(200, 40), 16);

        // Player 1 (AI Left) - left side
        TextMeshProUGUI playerInfo1 = CreateText(canvasObj.transform, "PlayerInfo_Left",
            "AI_Left\nCards: 17",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(100, 0), new Vector2(200, 60), 16);

        // Player 2 (AI Right) - right side
        TextMeshProUGUI playerInfo2 = CreateText(canvasObj.transform, "PlayerInfo_Right",
            "AI_Right\nCards: 17",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-100, 0), new Vector2(200, 60), 16);

        TextMeshProUGUI[] playerInfoTexts = { playerInfo0, playerInfo1, playerInfo2 };

        // ==================== Center Area ====================

        // Message text (center top)
        TextMeshProUGUI messageText = CreateText(canvasObj.transform, "MessageText",
            "Welcome to Dou Di Zhu!",
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f),
            Vector2.zero, new Vector2(600, 50), 24);

        // Last played cards display (center)
        TextMeshProUGUI lastPlayedText = CreateText(canvasObj.transform, "LastPlayedText",
            "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500, 80), 20);

        // ==================== Bid Panel ====================

        GameObject bidPanel = new GameObject("BidPanel");
        bidPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform bidPanelRect = bidPanel.AddComponent<RectTransform>();
        bidPanelRect.anchorMin = new Vector2(0.5f, 0.3f);
        bidPanelRect.anchorMax = new Vector2(0.5f, 0.3f);
        bidPanelRect.anchoredPosition = Vector2.zero;
        bidPanelRect.sizeDelta = new Vector2(400, 60);

        Button bidButton = CreateButton(bidPanel.transform, "BidButton",
            "Call Landlord", new Vector2(-110, 0), new Vector2(180, 50));
        Button noBidButton = CreateButton(bidPanel.transform, "NoBidButton",
            "Pass", new Vector2(110, 0), new Vector2(180, 50));

        // ==================== Play Panel ====================

        GameObject playPanel = new GameObject("PlayPanel");
        playPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform playPanelRect = playPanel.AddComponent<RectTransform>();
        playPanelRect.anchorMin = new Vector2(0.5f, 0.3f);
        playPanelRect.anchorMax = new Vector2(0.5f, 0.3f);
        playPanelRect.anchoredPosition = Vector2.zero;
        playPanelRect.sizeDelta = new Vector2(400, 60);

        Button playButton = CreateButton(playPanel.transform, "PlayButton",
            "Play", new Vector2(-110, 0), new Vector2(180, 50));
        Button passButton = CreateButton(playPanel.transform, "PassButton",
            "Pass", new Vector2(110, 0), new Vector2(180, 50));

        // ==================== Wire Everything Up ====================

        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();
        handView.Init(uiManager);
        uiManager.Init(handView, turnManager, bidManager, aiPlayer);
        uiManager.SetUIElements(
            playButton, passButton,
            bidButton, noBidButton,
            bidPanel, playPanel,
            messageText, lastPlayedText,
            playerInfoTexts
        );

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
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.15f, 0.1f); // Dark brown text

        return btn;
    }
}
