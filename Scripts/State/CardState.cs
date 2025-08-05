using System;

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
        public float VisualOffsetX { get; }
        public float VisualOffsetY { get; }
        
        // Resource data (from SpellCardResource)
        public string ResourceId { get; }
        public string CardName { get; }
        public string CardDescription { get; }
        
        public CardState(
            string cardId,
            bool isSelected = false,
            bool isDragging = false,
            int position = 0,
            float visualOffsetX = 0f,
            float visualOffsetY = 0f,
            string resourceId = null,
            string cardName = null,
            string cardDescription = null)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            IsSelected = isSelected;
            IsDragging = isDragging;
            Position = position;
            VisualOffsetX = visualOffsetX;
            VisualOffsetY = visualOffsetY;
            ResourceId = resourceId;
            CardName = cardName;
            CardDescription = cardDescription;
        }

        /// <summary>
        /// Creates a new CardState with updated selection status
        /// </summary>
        public CardState WithSelection(bool isSelected)
        {
            return new CardState(
                CardId,
                isSelected,
                IsDragging,
                Position,
                VisualOffsetX,
                VisualOffsetY,
                ResourceId,
                CardName,
                CardDescription);
        }

        /// <summary>
        /// Creates a new CardState with updated drag status
        /// </summary>
        public CardState WithDrag(bool isDragging)
        {
            return new CardState(
                CardId,
                IsSelected,
                isDragging,
                Position,
                VisualOffsetX,
                VisualOffsetY,
                ResourceId,
                CardName,
                CardDescription);
        }

        /// <summary>
        /// Creates a new CardState with updated position
        /// </summary>
        public CardState WithPosition(int position)
        {
            return new CardState(
                CardId,
                IsSelected,
                IsDragging,
                position,
                VisualOffsetX,
                VisualOffsetY,
                ResourceId,
                CardName,
                CardDescription);
        }

        /// <summary>
        /// Creates a new CardState with updated visual offset
        /// </summary>
        public CardState WithVisualOffset(float offsetX, float offsetY)
        {
            return new CardState(
                CardId,
                IsSelected,
                IsDragging,
                Position,
                offsetX,
                offsetY,
                ResourceId,
                CardName,
                CardDescription);
        }
        
        /// <summary>
        /// Creates a new CardState with updated resource data
        /// </summary>
        public CardState WithResourceData(string resourceId, string cardName, string cardDescription)
        {
            return new CardState(
                CardId,
                IsSelected,
                IsDragging,
                Position,
                VisualOffsetX,
                VisualOffsetY,
                resourceId,
                cardName,
                cardDescription);
        }

        public override bool Equals(object obj)
        {
            if (obj is CardState other)
            {
                return CardId == other.CardId &&
                       IsSelected == other.IsSelected &&
                       IsDragging == other.IsDragging &&
                       Position == other.Position &&
                       Math.Abs(VisualOffsetX - other.VisualOffsetX) < 0.001f &&
                       Math.Abs(VisualOffsetY - other.VisualOffsetY) < 0.001f &&
                       ResourceId == other.ResourceId &&
                       CardName == other.CardName &&
                       CardDescription == other.CardDescription;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(CardId, IsSelected, IsDragging, Position);
            hash = HashCode.Combine(hash, VisualOffsetX, VisualOffsetY);
            return HashCode.Combine(hash, ResourceId, CardName, CardDescription);
        }
    }
}