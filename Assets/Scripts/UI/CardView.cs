using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single card in the UI.
/// Loads card artwork from Resources/Sprites/ and handles selection state.
/// </summary>
public class CardView : MonoBehaviour
{
    // The card data this view represents
    private Card card;

    // Whether this card is currently selected by the player
    private bool isSelected = false;

    // UI components
    private Image cardImage;
    private RectTransform rectTransform;
    private Button button;

    // Visual constants
    private static readonly Color NORMAL_TINT = Color.white;
    private static readonly Color SELECTED_TINT = new Color(0.7f, 1f, 0.7f);  // Light green tint
    private static readonly float CARD_WIDTH = 110f;
    private static readonly float CARD_HEIGHT = 165f;
    private static readonly float SELECTED_OFFSET_Y = 20f;

    // Sprite cache: loaded once, shared by all CardView instances
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // Reference to parent hand view for selection callback
    private HandView parentHandView;

    /// <summary>
    /// Creates and initializes a CardView on a new GameObject.
    /// </summary>
    public static CardView Create(Card card, Transform parent, HandView handView)
    {
        GameObject cardObj = new GameObject($"Card_{card}");
        cardObj.transform.SetParent(parent, false);

        CardView view = cardObj.AddComponent<CardView>();
        view.card = card;
        view.parentHandView = handView;
        view.SetupUI();
        return view;
    }

    /// <summary>
    /// Creates a display-only card (no click interaction) for showing played cards.
    /// </summary>
    public static GameObject CreateDisplayCard(Card card, Transform parent, float width, float height)
    {
        GameObject cardObj = new GameObject($"Played_{card}");
        cardObj.transform.SetParent(parent, false);

        RectTransform rect = cardObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = cardObj.AddComponent<Image>();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;

        Sprite sprite = LoadSpriteForCard(card);
        if (sprite != null)
            img.sprite = sprite;

        return cardObj;
    }

    /// <summary>
    /// Static method to load a sprite for any card, using the shared cache.
    /// </summary>
    public static Sprite LoadSpriteForCard(Card card)
    {
        string spriteName = GetSpriteNameForCard(card);

        if (spriteCache.TryGetValue(spriteName, out Sprite cached))
            return cached;

        Texture2D tex = Resources.Load<Texture2D>($"Sprites/{spriteName}");
        if (tex == null)
        {
            Debug.LogWarning($"Card sprite not found: Sprites/{spriteName}");
            return null;
        }

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));
        spriteCache[spriteName] = sprite;
        return sprite;
    }

    /// <summary>
    /// Returns the sprite filename for a given card (static version).
    /// </summary>
    private static string GetSpriteNameForCard(Card card)
    {
        switch (card.Rank)
        {
            case Rank.Three: return "card_3";
            case Rank.Four: return "card_4";
            case Rank.Five: return "card_5";
            case Rank.Six: return "card_6";
            case Rank.Seven: return "card_7";
            case Rank.Eight: return "card_8";
            case Rank.Nine: return "card_9";
            case Rank.Ten: return "card_10";
            case Rank.Jack: return "card_J";
            case Rank.Queen: return "card_Q";
            case Rank.King: return "card_K";
            case Rank.Ace: return "card_A";
            case Rank.Two: return "card_2";
            case Rank.RedJoker: return "card_joker_red";
            case Rank.BlackJoker: return "card_joker_black";
            default: return "card_back";
        }
    }

    /// <summary>
    /// Builds the card's UI using the artwork sprite.
    /// </summary>
    private void SetupUI()
    {
        // RectTransform for sizing
        rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

        // Card image using artwork from Resources/Sprites/
        cardImage = gameObject.AddComponent<Image>();
        Sprite sprite = LoadCardSprite();
        if (sprite != null)
        {
            cardImage.sprite = sprite;
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = true;
        }
        cardImage.color = NORMAL_TINT;

        // Click button for selection
        button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// Loads the sprite for this card instance using the shared static loader.
    /// </summary>
    private Sprite LoadCardSprite()
    {
        return LoadSpriteForCard(card);
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
    /// Selected cards move up slightly and get a green tint.
    /// </summary>
    private void UpdateVisual()
    {
        cardImage.color = isSelected ? SELECTED_TINT : NORMAL_TINT;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = isSelected ? SELECTED_OFFSET_Y : 0f;
        rectTransform.anchoredPosition = pos;
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
