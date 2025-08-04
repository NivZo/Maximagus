using System;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state for a single card
    /// </summary>
    public class CardState
    {
        public string CardId { get; }
        public bool IsSelected { get; }
        public bool IsDragging { get; }
        public int Position { get; }
        public float VisualOffsetX { get; }
        public float VisualOffsetY { get; }

        public CardState(
            string cardId,
            bool isSelected = false,
            bool isDragging = false,
            int position = 0,
            float visualOffsetX = 0f,
            float visualOffsetY = 0f)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            IsSelected = isSelected;
            IsDragging = isDragging;
            Position = position;
            VisualOffsetX = visualOffsetX;
            VisualOffsetY = visualOffsetY;
        }

        /// <summary>
        /// Creates a new CardState with updated selection status
        /// </summary>
        public CardState WithSelection(bool isSelected)
        {
            return new CardState(CardId, isSelected, IsDragging, Position, VisualOffsetX, VisualOffsetY);
        }

        /// <summary>
        /// Creates a new CardState with updated drag status
        /// </summary>
        public CardState WithDrag(bool isDragging)
        {
            return new CardState(CardId, IsSelected, isDragging, Position, VisualOffsetX, VisualOffsetY);
        }

        /// <summary>
        /// Creates a new CardState with updated position
        /// </summary>
        public CardState WithPosition(int position)
        {
            return new CardState(CardId, IsSelected, IsDragging, position, VisualOffsetX, VisualOffsetY);
        }

        /// <summary>
        /// Creates a new CardState with updated visual offset
        /// </summary>
        public CardState WithVisualOffset(float offsetX, float offsetY)
        {
            return new CardState(CardId, IsSelected, IsDragging, Position, offsetX, offsetY);
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
                       Math.Abs(VisualOffsetY - other.VisualOffsetY) < 0.001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CardId, IsSelected, IsDragging, Position, VisualOffsetX, VisualOffsetY);
        }
    }
}