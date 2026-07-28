using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Displays and manages the player's hand of cards.
/// Handles card layout, selection tracking, and card removal after playing.
/// </summary>
public class HandView : MonoBehaviour
{
    // All card views currently displayed in the hand
    private List<CardView> cardViews = new List<CardView>();

    // Spacing between overlapping cards (each card is 120px wide, 55px shows enough of each card)
    private static readonly float CARD_SPACING = 55f;

    // Reference to the UI manager for notifying selection changes
    private GameUIManager uiManager;

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
