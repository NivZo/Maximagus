using System;
using Maximagus.Scripts.Spells.Abstractions;

namespace Scripts.State
{
    public class CardState
    {
        public string CardId { get; }
        public bool IsSelected { get; }
        public bool IsDragging { get; }
        public bool IsHovering { get; }
        public int Position { get; }
        public ContainerType ContainerType { get; }
        public SpellCardResource Resource { get; }

        public CardState(
            string cardId,
            SpellCardResource resource,
            bool isSelected = false,
            bool isDragging = false,
            bool isHovering = false,
            int position = 0,
            ContainerType containerType = ContainerType.Hand)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            Resource = resource;
            IsSelected = isSelected;
            IsDragging = isDragging;
            IsHovering = isHovering;
            Position = position;
            ContainerType = containerType;
        }

        public CardState WithSelection(bool isSelected)
        {
            return new CardState(CardId, Resource, isSelected, IsDragging, IsHovering, Position, ContainerType);
        }

        public CardState WithDrag(bool isDragging)
        {
            return new CardState(CardId, Resource, IsSelected, isDragging, IsHovering, Position, ContainerType);
        }

        public CardState WithHover(bool isHovering)
        {
            return new CardState(CardId, Resource, IsSelected, IsDragging, isHovering, Position, ContainerType);
        }

        public CardState WithPosition(int position)
        {
            return new CardState(CardId, Resource, IsSelected, IsDragging, IsHovering, position, ContainerType);
        }

        public CardState WithContainerType(ContainerType containerType)
        {
            return new CardState(CardId, Resource, IsSelected, IsDragging, IsHovering, Position, containerType);
        }

        public override bool Equals(object obj)
        {
            if (obj is CardState other)
            {
                return CardId == other.CardId &&
                       IsSelected == other.IsSelected &&
                       IsDragging == other.IsDragging &&
                       IsHovering == other.IsHovering &&
                       Position == other.Position &&
                       ContainerType == other.ContainerType &&
                       Resource.GetRid() == other.Resource.GetRid();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CardId, IsSelected, IsDragging, IsHovering, Position, Resource, ContainerType);
        }
    }
}