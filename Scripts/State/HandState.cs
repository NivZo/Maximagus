using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state for the player's hand
    /// </summary>
    public class HandState
    {
        public IReadOnlyList<CardState> Cards { get; }
        public IReadOnlyList<string> SelectedCardIds { get; }
        public int MaxHandSize { get; }
        public bool IsLocked { get; }

        public HandState(
            IEnumerable<CardState> cards = null,
            IEnumerable<string> selectedCardIds = null,
            int maxHandSize = 10,
            bool isLocked = false)
        {
            Cards = (cards ?? Enumerable.Empty<CardState>()).ToList().AsReadOnly();
            SelectedCardIds = (selectedCardIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            MaxHandSize = maxHandSize;
            IsLocked = isLocked;
        }

        /// <summary>
        /// Gets the currently selected cards
        /// </summary>
        public IEnumerable<CardState> SelectedCards => 
            Cards.Where(card => SelectedCardIds.Contains(card.CardId));

        /// <summary>
        /// Gets the count of selected cards
        /// </summary>
        public int SelectedCount => SelectedCardIds.Count;

        /// <summary>
        /// Gets the total number of cards in hand
        /// </summary>
        public int Count => Cards.Count;

        /// <summary>
        /// Gets the currently dragging card (if any)
        /// </summary>
        public CardState DraggingCard => Cards.FirstOrDefault(card => card.IsDragging);

        /// <summary>
        /// Gets whether any card is currently being dragged
        /// </summary>
        public bool HasDraggingCard => Cards.Any(card => card.IsDragging);

        /// <summary>
        /// Creates a new HandState with an added card
        /// </summary>
        public HandState WithAddedCard(CardState card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (Count >= MaxHandSize) throw new InvalidOperationException("Hand is at maximum capacity");

            var newCards = Cards.ToList();
            newCards.Add(card);
            return new HandState(newCards, SelectedCardIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with a removed card
        /// </summary>
        public HandState WithRemovedCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var newCards = Cards.Where(c => c.CardId != cardId).ToList();
            var newSelectedIds = SelectedCardIds.Where(id => id != cardId).ToList();
            return new HandState(newCards, newSelectedIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with updated card state
        /// </summary>
        public HandState WithUpdatedCard(CardState updatedCard)
        {
            if (updatedCard == null) throw new ArgumentNullException(nameof(updatedCard));

            var newCards = Cards.Select(card => 
                card.CardId == updatedCard.CardId ? updatedCard : card).ToList();
            return new HandState(newCards, SelectedCardIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with a card selected/deselected
        /// FIXED: Updates both SelectedCardIds AND individual CardState.IsSelected properties
        /// </summary>
        public HandState WithCardSelection(string cardId, bool isSelected)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var newSelectedIds = SelectedCardIds.ToList();
            var isCurrentlySelected = newSelectedIds.Contains(cardId);

            if (isSelected && !isCurrentlySelected)
            {
                newSelectedIds.Add(cardId);
            }
            else if (!isSelected && isCurrentlySelected)
            {
                newSelectedIds.Remove(cardId);
            }

            // CRITICAL FIX: Update individual CardState objects to match the selection
            var newCards = Cards.Select(card =>
            {
                if (card.CardId == cardId)
                {
                    // Update the target card's IsSelected property
                    return new CardState(card.CardId, isSelected, card.IsDragging, card.Position);
                }
                return card;
            }).ToList();

            return new HandState(newCards, newSelectedIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with a card's dragging status updated
        /// </summary>
        public HandState WithCardDragging(string cardId, bool isDragging)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var newCards = Cards.Select(card =>
            {
                if (card.CardId == cardId)
                {
                    // Update the target card's dragging status
                    return new CardState(card.CardId, card.IsSelected, isDragging, card.Position);
                }
                else if (isDragging && card.IsDragging)
                {
                    // If setting a card to dragging, clear any other dragging cards
                    return new CardState(card.CardId, card.IsSelected, false, card.Position);
                }
                return card;
            }).ToList();

            return new HandState(newCards, SelectedCardIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with all cards deselected
        /// FIXED: Updates both SelectedCardIds AND individual CardState.IsSelected properties
        /// </summary>
        public HandState WithClearedSelection()
        {
            // Update all CardState objects to have IsSelected = false
            var newCards = Cards.Select(card => 
                new CardState(card.CardId, false, card.IsDragging, card.Position)).ToList();
            
            return new HandState(newCards, Enumerable.Empty<string>(), MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with reordered cards
        /// </summary>
        public HandState WithReorderedCards(IEnumerable<string> cardIdOrder)
        {
            if (cardIdOrder == null) throw new ArgumentNullException(nameof(cardIdOrder));

            var cardDict = Cards.ToDictionary(c => c.CardId);
            var reorderedCards = cardIdOrder
                .Where(id => cardDict.ContainsKey(id))
                .Select(id => cardDict[id])
                .ToList();

            // Add any cards that weren't in the order list
            var missingCards = Cards.Where(c => !cardIdOrder.Contains(c.CardId));
            reorderedCards.AddRange(missingCards);

            return new HandState(reorderedCards, SelectedCardIds, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with updated lock status
        /// </summary>
        public HandState WithLockStatus(bool isLocked)
        {
            return new HandState(Cards, SelectedCardIds, MaxHandSize, isLocked);
        }

        /// <summary>
        /// Validates that the hand state is consistent
        /// ENHANCED: Also validates that SelectedCardIds matches individual CardState.IsSelected properties
        /// </summary>
        public bool IsValid()
        {
            // Check that all selected card IDs exist in the hand
            var basicValid = SelectedCardIds.All(id => Cards.Any(card => card.CardId == id)) &&
                   Cards.Count <= MaxHandSize &&
                   Cards.Select(c => c.CardId).Distinct().Count() == Cards.Count; // No duplicate card IDs

            // Check that only one card can be dragging at a time
            var draggingCount = Cards.Count(card => card.IsDragging);
            var draggingValid = draggingCount <= 1;

            // CRITICAL: Check that SelectedCardIds matches individual CardState.IsSelected properties
            var selectionConsistent = true;
            foreach (var card in Cards)
            {
                var isInSelectedList = SelectedCardIds.Contains(card.CardId);
                if (card.IsSelected != isInSelectedList)
                {
                    selectionConsistent = false;
                    break;
                }
            }

            return basicValid && draggingValid && selectionConsistent;
        }

        public override bool Equals(object obj)
        {
            if (obj is HandState other)
            {
                return Cards.SequenceEqual(other.Cards) &&
                       SelectedCardIds.SequenceEqual(other.SelectedCardIds) &&
                       MaxHandSize == other.MaxHandSize &&
                       IsLocked == other.IsLocked;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Cards.GetHashCode(),
                SelectedCardIds.GetHashCode(),
                MaxHandSize,
                IsLocked);
        }
    }
}