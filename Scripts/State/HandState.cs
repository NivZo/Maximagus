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
        public int MaxHandSize { get; }
        public bool IsLocked { get; }

        public HandState(
            IEnumerable<CardState> cards = null,
            int maxHandSize = 10,
            bool isLocked = false)
        {
            Cards = (cards ?? Enumerable.Empty<CardState>()).ToList().AsReadOnly();
            MaxHandSize = maxHandSize;
            IsLocked = isLocked;
        }

        public IEnumerable<CardState> SelectedCards => Cards.Where(card => card.IsSelected);

        public int SelectedCount => SelectedCards.Count();

        public int Count => Cards.Count;

        public CardState DraggingCard => Cards.FirstOrDefault(card => card.IsDragging);

        public bool HasDraggingCard => Cards.Any(card => card.IsDragging);

        public HandState WithAddedCard(CardState card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (Count >= MaxHandSize) throw new InvalidOperationException("Hand is at maximum capacity");

            var newCards = Cards.ToList();
            newCards.Add(card);
            newCards = newCards
                .Select((c, i) => c.WithPosition(i))
                .ToList();
            return new HandState(newCards, MaxHandSize, IsLocked);
        }

        public HandState WithRemovedCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var newCards = Cards
                .Where(c => c.CardId != cardId)
                .Select((c, i) => c.WithPosition(i))
                .ToList();
            return new HandState(newCards, MaxHandSize, IsLocked);
        }
        
        public HandState WithRemovedCards(IEnumerable<string> cardIds)
        {
            if (cardIds == null) throw new ArgumentNullException(nameof(cardIds));

            var newCards = Cards
                .Where(c => !cardIds.Contains(c.CardId))
                .Select((c, i) => c.WithPosition(i))
                .ToList();
            return new HandState(newCards, MaxHandSize, IsLocked);
        }

        public HandState WithUpdatedCard(CardState updatedCard)
        {
            if (updatedCard == null) throw new ArgumentNullException(nameof(updatedCard));

            var newCards = Cards.Select(card =>
                card.CardId == updatedCard.CardId ? updatedCard : card).ToList();
            return new HandState(newCards, MaxHandSize, IsLocked);
        }

        public HandState WithCardSelection(string cardId, bool isSelected)
        {
            var card = Cards.FirstOrDefault(c => c.CardId == cardId);
            if (card == null) throw new ArgumentException($"Card with ID {cardId} not found in hand");

            var newCards = Cards.Select(card =>
            {
                if (card.CardId == cardId)
                {
                    return card.WithSelection(isSelected);
                }
                return card;
            }).ToList();

            return new HandState(newCards, MaxHandSize, IsLocked);
        }

        public HandState WithCardDragging(string cardId, bool isDragging)
        {
            var card = Cards.FirstOrDefault(c => c.CardId == cardId);
            if (card == null) throw new ArgumentException($"Card with ID {cardId} not found in hand");

            var newCards = Cards.Select(card =>
            {
                if (card.CardId == cardId)
                {
                    return card.WithDrag(isDragging);
                }
                return card;
            }).ToList();

            return new HandState(newCards, MaxHandSize, IsLocked);
        }

        public HandState WithClearedSelection()
        {
            // Update all CardState objects to have IsSelected = false
            var newCards = Cards.Select(card => card.WithSelection(false)).ToList();
            return new HandState(newCards, MaxHandSize, IsLocked);
        }

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

            return new HandState(reorderedCards, MaxHandSize, IsLocked);
        }

        /// <summary>
        /// Creates a new HandState with updated lock status
        /// </summary>
        public HandState WithLockStatus(bool isLocked)
        {
            return new HandState(Cards, MaxHandSize, isLocked);
        }

        /// <summary>
        /// Validates that the hand state is consistent
        /// ENHANCED: Also validates that SelectedCardIds matches individual CardState.IsSelected properties
        /// </summary>
        public bool IsValid()
        {
            var basicValid = Cards.Count <= MaxHandSize &&
                   Cards.Select(c => c.CardId).Distinct().Count() == Cards.Count;

            var draggingCount = Cards.Count(card => card.IsDragging);
            var draggingValid = draggingCount <= 1;

            return basicValid && draggingValid;
        }

        public override bool Equals(object obj)
        {
            if (obj is HandState other)
            {
                return Cards.SequenceEqual(other.Cards) &&
                       MaxHandSize == other.MaxHandSize &&
                       IsLocked == other.IsLocked;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Cards.GetHashCode(),
                MaxHandSize,
                IsLocked);
        }
    }
}