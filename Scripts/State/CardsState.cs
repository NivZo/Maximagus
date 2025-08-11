using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.State
{
    /// Immutable state for all cards across all containers (Hand, Played, Discarded).
    /// This replaces storing cards inside HandState and becomes the single source of truth for card placement.
    public class CardsState
    {
        public IReadOnlyList<CardState> Cards { get; }

        public CardsState(IEnumerable<CardState> cards = null)
        {
            Cards = (cards ?? Enumerable.Empty<CardState>()).ToList().AsReadOnly();
        }

        /* Queries (position-ordered) */
        public IEnumerable<CardState> InContainer(ContainerType container) =>
            Cards.Where(c => c.ContainerType == container).OrderBy(c => c.Position);
        
        public IEnumerable<CardState> HandCards => InContainer(ContainerType.Hand);
        public IEnumerable<CardState> PlayedCards => InContainer(ContainerType.PlayedCards);
        public IEnumerable<CardState> DiscardedCards => InContainer(ContainerType.DiscardedCards);
        
        public IEnumerable<CardState> SelectedInHand => HandCards.Where(c => c.IsSelected);
        public int InHandCount => HandCards.Count();

        public CardState DraggingInHand => HandCards.FirstOrDefault(c => c.IsDragging);
        public bool HasDragging => Cards.Any(c => c.IsDragging);

        // Mutations (immutable - return new instance)
        public CardsState WithAddedCard(CardState card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));

            var list = Cards.ToList();
            list.Add(card);
            list = ReindexPositions(list, card.ContainerType);
            return new CardsState(list);
        }

        public CardsState WithRemovedCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var removedCard = Cards.FirstOrDefault(c => c.CardId == cardId);
            var list = Cards.Where(c => c.CardId != cardId).ToList();

            if (removedCard != null)
            {
                list = ReindexPositions(list, removedCard.ContainerType);
            }

            return new CardsState(list);
        }

        public CardsState WithRemovedCards(IEnumerable<string> cardIds)
        {
            if (cardIds == null) throw new ArgumentNullException(nameof(cardIds));
            var idSet = cardIds.ToHashSet();

            var list = Cards.Where(c => !idSet.Contains(c.CardId)).ToList();

            // Reindex all containers since multiple may have changed
            list = ReindexPositions(list, ContainerType.Hand);
            list = ReindexPositions(list, ContainerType.PlayedCards);
            list = ReindexPositions(list, ContainerType.DiscardedCards);

            return new CardsState(list);
        }

        public CardsState WithCardSelection(string cardId, bool isSelected)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var list = Cards.Select(c => c.CardId == cardId ? c.WithSelection(isSelected) : c).ToList();
            return new CardsState(list);
        }

        public CardsState WithCardDragging(string cardId, bool isDragging)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            var list = Cards.Select(c => c.CardId == cardId ? c.WithDrag(isDragging) : c).ToList();
            return new CardsState(list);
        }

        public CardsState WithClearedSelection()
        {
            var list = Cards.Select(c => c.WithSelection(false)).ToList();
            return new CardsState(list);
        }

        public CardsState WithMovedToContainer(string cardId, ContainerType target)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));
            return WithMovedToContainer(new[] { cardId }, target);
        }

        public CardsState WithMovedToContainer(IEnumerable<string> cardIds, ContainerType target)
        {
            if (cardIds == null) throw new ArgumentNullException(nameof(cardIds));

            var idSet = cardIds.ToHashSet();
            var list = Cards.Select(c => idSet.Contains(c.CardId) ? c.WithContainerType(target) : c).ToList();

            // Reindex all containers since some cards moved between them
            list = ReindexPositions(list, ContainerType.Hand);
            list = ReindexPositions(list, ContainerType.PlayedCards);
            list = ReindexPositions(list, ContainerType.DiscardedCards);

            return new CardsState(list);
        }

        public CardsState WithReorderedHandCards(IReadOnlyList<string> newOrder)
        {
            if (newOrder == null) throw new ArgumentNullException(nameof(newOrder));

            // Build the reordered list of hand cards
            var handDict = HandCards.ToDictionary(c => c.CardId);
            var reordered = new List<CardState>(newOrder.Count);

            foreach (var id in newOrder)
            {
                if (handDict.TryGetValue(id, out var c))
                {
                    reordered.Add(c);
                }
            }

            // Add any hand cards that weren't included in the new order
            foreach (var c in HandCards)
            {
                if (!reordered.Any(x => x.CardId == c.CardId))
                {
                    reordered.Add(c);
                }
            }

            /* Apply new sequential positions just for hand cards */
            var list = Cards.ToList();
            for (int i = 0; i < reordered.Count; i++)
            {
                var c = reordered[i];
                var idx = list.FindIndex(x => x.CardId == c.CardId);
                if (idx >= 0)
                {
                    list[idx] = c.WithPosition(i);
                }
            }
            
            /* Normalize positions within Hand to ensure 0..n-1 sequence */
            list = ReindexPositions(list, ContainerType.Hand);
            
            return new CardsState(list);
        }

        public bool IsValid()
        {
            try
            {
                var draggingCount = Cards.Count(c => c.IsDragging);
                if (draggingCount > 1) return false;

                // Card IDs must be unique
                if (Cards.Select(c => c.CardId).Distinct().Count() != Cards.Count) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<CardState> ReindexPositions(List<CardState> cards, ContainerType container)
        {
            // Only reindex cards inside the specified container, keep others unchanged
            var list = cards.ToList();

            var ordered = list
                .Where(c => c.ContainerType == container)
                .OrderBy(c => c.Position)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                var idx = list.FindIndex(x => x.CardId == c.CardId);
                if (idx >= 0)
                {
                    list[idx] = c.WithPosition(i);
                }
            }

            return list;
        }

        public override bool Equals(object obj)
        {
            if (obj is CardsState other)
            {
                return Cards.SequenceEqual(other.Cards);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cards);
        }
    }
}