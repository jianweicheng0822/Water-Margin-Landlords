using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Displays and manages the player's hand of cards.
/// Handles card layout, selection via click and drag, and card removal after playing.
///
/// Selection modes:
/// - Click: tap a card to toggle its selection.
/// - Drag: press and drag across multiple cards to toggle them all in one sweep.
///   Cards already toggled during the current drag are tracked to avoid re-toggling.
/// </summary>
public class HandView : MonoBehaviour
{
    // All card views currently displayed in the hand
    private List<CardView> cardViews = new List<CardView>();

    // Spacing between overlapping cards (each card is 110px wide, 65px shows rank and part of art)
    private static readonly float CARD_SPACING = 65f;

    // Minimum mouse movement (in pixels) to distinguish drag from click
    private static readonly float DRAG_THRESHOLD = 5f;

    // Reference to the UI manager for notifying selection changes
    private GameUIManager uiManager;

    // Drag state tracking
    private bool isMouseDown = false;
    private Vector2 mouseDownPosition;
    private bool isDragging = false;

    // Cards already toggled during the current drag (prevents re-toggling on hover)
    private HashSet<CardView> toggledThisDrag = new HashSet<CardView>();

    /// <summary>
    /// Sets the UI manager reference for callbacks.
    /// </summary>
    public void Init(GameUIManager manager)
    {
        uiManager = manager;
    }

    /// <summary>
    /// Displays a list of cards as the player's hand.
    /// Clears any existing cards first.
    /// </summary>
    public void ShowHand(List<Card> cards)
    {
        ClearHand();

        for (int i = 0; i < cards.Count; i++)
        {
            CardView view = CardView.Create(cards[i], transform, this);
            RectTransform rect = view.GetComponent<RectTransform>();

            // Position cards in a horizontal row, centered
            float totalWidth = (cards.Count - 1) * CARD_SPACING;
            float startX = -totalWidth / 2f;
            rect.anchoredPosition = new Vector2(startX + i * CARD_SPACING, 0);

            cardViews.Add(view);
        }
    }

    /// <summary>
    /// Monitors mouse input each frame for click and drag selection.
    /// Uses new Input System (Mouse.current) instead of legacy Input class.
    /// </summary>
    private void Update()
    {
        if (cardViews.Count == 0) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // Mouse button pressed - start tracking
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Only start if the press is on a card
            CardView hitCard = GetCardUnderMouse(mousePos);
            if (hitCard != null)
            {
                isMouseDown = true;
                mouseDownPosition = mousePos;
                isDragging = false;
                toggledThisDrag.Clear();
            }
        }

        // Mouse held down - check for drag
        if (isMouseDown)
        {
            float distance = Vector2.Distance(mouseDownPosition, mousePos);

            // Once mouse moves past threshold, enter drag mode
            if (!isDragging && distance > DRAG_THRESHOLD)
            {
                isDragging = true;
            }

            // While dragging, toggle any card under the mouse (once per card per drag)
            if (isDragging)
            {
                CardView hitCard = GetCardUnderMouse(mousePos);
                if (hitCard != null && !toggledThisDrag.Contains(hitCard))
                {
                    hitCard.ToggleSelection();
                    toggledThisDrag.Add(hitCard);
                }
            }
        }

        // Mouse button released
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (isMouseDown)
            {
                // If it wasn't a drag, treat as a click - toggle the card under the mouse
                if (!isDragging)
                {
                    CardView hitCard = GetCardUnderMouse(mousePos);
                    if (hitCard != null)
                    {
                        hitCard.ToggleSelection();
                    }
                }

                // Reset drag state
                isMouseDown = false;
                isDragging = false;
                toggledThisDrag.Clear();
            }
        }
    }

    /// <summary>
    /// Finds the topmost card under the given screen position using UI raycast.
    /// Results from EventSystem are sorted front-to-back, so the first matching
    /// card is the visually topmost one.
    /// </summary>
    private CardView GetCardUnderMouse(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Find the first result that belongs to one of our card views
        // Results are sorted front-to-back by the event system
        foreach (RaycastResult result in results)
        {
            CardView cardView = result.gameObject.GetComponent<CardView>();
            if (cardView != null && cardViews.Contains(cardView))
            {
                return cardView;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all currently selected cards.
    /// </summary>
    public List<Card> GetSelectedCards()
    {
        return cardViews
            .Where(cv => cv.IsSelected())
            .Select(cv => cv.GetCard())
            .ToList();
    }

    /// <summary>
    /// Removes the specified cards from the hand display.
    /// Called after cards are successfully played.
    /// </summary>
    public void RemoveCards(List<Card> playedCards)
    {
        foreach (Card card in playedCards)
        {
            CardView view = cardViews.FirstOrDefault(
                cv => cv.GetCard().Rank == card.Rank && cv.GetCard().Suit == card.Suit
            );
            if (view != null)
            {
                cardViews.Remove(view);
                Destroy(view.gameObject);
            }
        }

        // Re-layout remaining cards
        RelayoutCards();
    }

    /// <summary>
    /// Deselects all cards in the hand.
    /// </summary>
    public void DeselectAll()
    {
        foreach (CardView view in cardViews)
        {
            view.Deselect();
        }
    }

    /// <summary>
    /// Clears all card views from the hand.
    /// </summary>
    public void ClearHand()
    {
        foreach (CardView view in cardViews)
        {
            Destroy(view.gameObject);
        }
        cardViews.Clear();
    }

    /// <summary>
    /// Repositions all cards evenly after some have been removed.
    /// </summary>
    private void RelayoutCards()
    {
        float totalWidth = (cardViews.Count - 1) * CARD_SPACING;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardViews.Count; i++)
        {
            RectTransform rect = cardViews[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * CARD_SPACING, 0);
        }
    }

    /// <summary>
    /// Called by CardView when a card's selection state changes.
    /// Forwards to GameUIManager to update button states.
    /// </summary>
    public void OnCardSelectionChanged()
    {
        uiManager?.OnSelectionChanged();
    }
}
