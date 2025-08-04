using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to reorder cards in the player's hand
    /// </summary>
    public class ReorderCardsCommand : IGameCommand
    {
        private readonly IReadOnlyList<string> _newCardOrder;

        public ReorderCardsCommand(IEnumerable<string> newCardOrder)
        {
            if (newCardOrder == null) throw new ArgumentNullException(nameof(newCardOrder));
            _newCardOrder = newCardOrder.ToList().AsReadOnly();
            
            if (_newCardOrder.Count == 0)
                throw new ArgumentException("Card order cannot be empty", nameof(newCardOrder));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Can only reorder during phases that allow player action
            if (!currentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            // All cards in the new order must exist in the current hand
            var currentCardIds = currentState.Hand.Cards.Select(c => c.CardId).ToHashSet();
            var newOrderSet = _newCardOrder.ToHashSet();

            // Check that all cards in new order exist in current hand
            if (!newOrderSet.IsSubsetOf(currentCardIds))
                return false;

            // Check that we're not missing any cards (all current cards should be in new order)
            // Allow partial reordering - cards not in the order will be appended
            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute ReorderCardsCommand");

            // Reorder the cards in the hand
            var newHandState = currentState.Hand.WithReorderedCards(_newCardOrder);

            // Return new game state with reordered hand
            return currentState.WithHand(newHandState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // For undo, restore the original card order
            var originalOrder = previousState.Hand.Cards.Select(c => c.CardId);
            return new ReorderCardsCommand(originalOrder);
        }

        public string GetDescription()
        {
            var cardCount = _newCardOrder.Count;
            var firstFew = string.Join(", ", _newCardOrder.Take(3));
            var suffix = cardCount > 3 ? "..." : "";
            return $"Reorder cards: [{firstFew}{suffix}] ({cardCount} cards)";
        }
    }
}