using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual representation of a single card in the UI.
/// Handles display, selection state, and click interaction.
/// </summary>
public class CardView : MonoBehaviour
{
    // The card data this view represents
    private Card card;

    // Whether this card is currently selected by the player
    private bool isSelected = false;

    // UI components
    private Image backgroundImage;
    private TextMeshProUGUI rankText;
    private TextMeshProUGUI suitText;
    private Button button;
    private RectTransform rectTransform;

    // Visual constants
    private static readonly Color NORMAL_COLOR = Color.white;
    private static readonly Color SELECTED_COLOR = new Color(0.8f, 1f, 0.8f);  // Light green
    private static readonly Color RED_SUIT_COLOR = new Color(0.8f, 0.1f, 0.1f);
    private static readonly Color BLACK_SUIT_COLOR = new Color(0.1f, 0.1f, 0.1f);
    private static readonly float CARD_WIDTH = 80f;
    private static readonly float CARD_HEIGHT = 120f;
    private static readonly float SELECTED_OFFSET_Y = 20f;

    // Reference to parent hand view for selection callback
    private HandView parentHandView;

    /// <summary>
    /// Creates and initializes a CardView on a new GameObject.
    /// </summary>
    public static CardView Create(Card card, Transform parent, HandView handView)
    {
        // Create card game object
        GameObject cardObj = new GameObject($"Card_{card}");
        cardObj.transform.SetParent(parent, false);

        CardView view = cardObj.AddComponent<CardView>();
        view.card = card;
        view.parentHandView = handView;
        view.SetupUI();
        return view;
    }

    /// <summary>
    /// Builds the card's UI components programmatically.
    /// </summary>
    private void SetupUI()
    {
        // RectTransform for sizing
        rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

        // Background image (white card)
        backgroundImage = gameObject.AddComponent<Image>();
        backgroundImage.color = NORMAL_COLOR;

        // Click button
        button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(OnClick);

        // Rank text (top-left)
        GameObject rankObj = new GameObject("RankText");
        rankObj.transform.SetParent(transform, false);
        rankText = rankObj.AddComponent<TextMeshProUGUI>();
        rankText.text = GetRankDisplay();
        rankText.fontSize = 24;
        rankText.fontStyle = FontStyles.Bold;
        rankText.alignment = TextAlignmentOptions.TopLeft;
        rankText.color = GetCardColor();
        RectTransform rankRect = rankText.GetComponent<RectTransform>();
        rankRect.anchorMin = new Vector2(0, 0);
        rankRect.anchorMax = new Vector2(1, 1);
        rankRect.offsetMin = new Vector2(5, 5);
        rankRect.offsetMax = new Vector2(-5, -5);

        // Suit text (center)
        GameObject suitObj = new GameObject("SuitText");
        suitObj.transform.SetParent(transform, false);
        suitText = suitObj.AddComponent<TextMeshProUGUI>();
        suitText.text = GetSuitDisplay();
        suitText.fontSize = 32;
        suitText.alignment = TextAlignmentOptions.Center;
        suitText.color = GetCardColor();
        RectTransform suitRect = suitText.GetComponent<RectTransform>();
        suitRect.anchorMin = new Vector2(0, 0);
        suitRect.anchorMax = new Vector2(1, 1);
        suitRect.offsetMin = new Vector2(5, 5);
        suitRect.offsetMax = new Vector2(-5, -5);
    }

    /// <summary>
    /// Handles card click - toggles selection state.
    /// </summary>
    private void OnClick()
    {
        isSelected = !isSelected;
        UpdateVisual();
        parentHandView?.OnCardSelectionChanged();
    }

    /// <summary>
    /// Updates the card's visual appearance based on selection state.
    /// Selected cards move up slightly and change color.
    /// </summary>
    private void UpdateVisual()
    {
        backgroundImage.color = isSelected ? SELECTED_COLOR : NORMAL_COLOR;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = isSelected ? SELECTED_OFFSET_Y : 0f;
        rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// Returns the display string for the card's rank.
    /// </summary>
    private string GetRankDisplay()
    {
        switch (card.Rank)
        {
            case Rank.Three: return "3";
            case Rank.Four: return "4";
            case Rank.Five: return "5";
            case Rank.Six: return "6";
            case Rank.Seven: return "7";
            case Rank.Eight: return "8";
            case Rank.Nine: return "9";
            case Rank.Ten: return "10";
            case Rank.Jack: return "J";
            case Rank.Queen: return "Q";
            case Rank.King: return "K";
            case Rank.Ace: return "A";
            case Rank.Two: return "2";
            case Rank.BlackJoker: return "\u5c0f";  // 小
            case Rank.RedJoker: return "\u5927";    // 大
            default: return "?";
        }
    }

    /// <summary>
    /// Returns the display symbol for the card's suit.
    /// </summary>
    private string GetSuitDisplay()
    {
        switch (card.Suit)
        {
            case Suit.Spade: return "\u2660";   // ♠
            case Suit.Heart: return "\u2665";   // ♥
            case Suit.Diamond: return "\u2666"; // ♦
            case Suit.Club: return "\u2663";    // ♣
            default: return "\u2605";           // ★ for jokers
        }
    }

    /// <summary>
    /// Returns the text color based on suit (red for hearts/diamonds, black for others).
    /// </summary>
    private Color GetCardColor()
    {
        if (card.Rank == Rank.RedJoker)
            return RED_SUIT_COLOR;
        if (card.Rank == Rank.BlackJoker)
            return BLACK_SUIT_COLOR;
        if (card.Suit == Suit.Heart || card.Suit == Suit.Diamond)
            return RED_SUIT_COLOR;
        return BLACK_SUIT_COLOR;
    }

    // ==================== Public Accessors ====================

    public Card GetCard() => card;
    public bool IsSelected() => isSelected;

    /// <summary>
    /// Deselects the card and resets visual state.
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
    }
}
