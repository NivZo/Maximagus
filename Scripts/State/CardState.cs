using System;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state for a single card
    /// </summary>
    public class CardState
    {
        // Identity
        public string CardId { get; }
        
        // State
        public bool IsSelected { get; }
        public bool IsDragging { get; }
        public int Position { get; }

        public SpellCardResource Resource { get; }

        public CardState(
            string cardId,
            SpellCardResource resource,
            bool isSelected = false,
            bool isDragging = false,
            int position = 0)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            Resource = resource;
            IsSelected = isSelected;
            IsDragging = isDragging;
            Position = position;
        }

        /// <summary>
        /// Creates a new CardState with updated selection status
        /// </summary>
        public CardState WithSelection(bool isSelected)
        {
            return new CardState(
                CardId,
                Resource,
                isSelected,
                IsDragging,
                Position);
        }

        /// <summary>
        /// Creates a new CardState with updated drag status
        /// </summary>
        public CardState WithDrag(bool isDragging)
        {
            return new CardState(
                CardId,
                Resource,
                IsSelected,
                isDragging,
                Position);
        }

        /// <summary>
        /// Creates a new CardState with updated position
        /// </summary>
        public CardState WithPosition(int position)
        {
            return new CardState(
                CardId,
                Resource,
                IsSelected,
                IsDragging,
                position);
        }

        /// <summary>
        /// Creates a new CardState with updated visual offset
        /// </summary>
        public CardState WithVisualOffset(float offsetX, float offsetY)
        {
            return new CardState(
                CardId,
                Resource,
                IsSelected,
                IsDragging,
                Position);
        }
        
        /// <summary>
        /// Creates a new CardState with updated resource data
        /// </summary>
        public CardState WithResourceData(string resourceId, string cardName, string cardDescription)
        {
            return new CardState(
                CardId,
                Resource,
                IsSelected,
                IsDragging,
                Position);
        }

        public override bool Equals(object obj)
        {
            if (obj is CardState other)
            {
                return CardId == other.CardId &&
                       IsSelected == other.IsSelected &&
                       IsDragging == other.IsDragging &&
                       Position == other.Position &&
                       Resource.GetRid() == other.Resource.GetRid();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CardId, IsSelected, IsDragging, Position, Resource);
        }
    }
}