using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to reorder cards in the player's hand
    /// </summary>
    public class ReorderCardsCommand : GameCommand
    {
        private readonly IReadOnlyList<string> _newCardOrder;

        public ReorderCardsCommand(IEnumerable<string> newCardOrder) : base()
        {
            if (newCardOrder == null) throw new ArgumentNullException(nameof(newCardOrder));
            _newCardOrder = newCardOrder.ToList().AsReadOnly();
            
            if (_newCardOrder.Count == 0)
                throw new ArgumentException("Card order cannot be empty", nameof(newCardOrder));
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;

            // Can only reorder during phases that allow player action
            if (!_commandProcessor.CurrentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            // All cards in the new order must exist in the current hand
            var currentCardIds = _commandProcessor.CurrentState.Hand.Cards.Select(c => c.CardId).ToHashSet();
            var newOrderSet = _newCardOrder.ToHashSet();

            // Check that all cards in new order exist in current hand
            if (!newOrderSet.IsSubsetOf(currentCardIds))
                return false;

            // Check that we're not missing any cards (all current cards should be in new order)
            // Allow partial reordering - cards not in the order will be appended
            return true;
        }

        public override CommandResult ExecuteWithResult()
        {
            var currentState = _commandProcessor.CurrentState;
            var currentHand = currentState.Hand;
            var cardDict = currentHand.Cards.ToDictionary(c => c.CardId);
            
            // Create new cards with updated positions
            var reorderedCards = new List<CardState>();
            
            for (int i = 0; i < _newCardOrder.Count; i++)
            {
                var cardId = _newCardOrder[i];
                if (cardDict.TryGetValue(cardId, out var card))
                {
                    // Update the card's position to match its new index
                    var updatedCard = card.WithPosition(i);
                    reorderedCards.Add(updatedCard);
                }
            }
            
            // Add any cards that weren't in the order list
            var missingCards = currentHand.Cards.Where(c => !_newCardOrder.Contains(c.CardId));
            foreach (var missingCard in missingCards)
            {
                var updatedCard = missingCard.WithPosition(reorderedCards.Count);
                reorderedCards.Add(updatedCard);
            }
            
            // Create new hand state with reordered cards that have updated positions
            var newHandState = new HandState(reorderedCards, currentHand.MaxHandSize, currentHand.IsLocked);
            var newState = currentState.WithHand(newHandState);

            return CommandResult.Success(newState);
        }

        public override string GetDescription()
        {
            var cardCount = _newCardOrder.Count;
            var firstFew = string.Join(", ", _newCardOrder.Take(3));
            var suffix = cardCount > 3 ? "..." : "";
            return $"Reorder cards with positions: [{firstFew}{suffix}] ({cardCount} cards)";
        }
    }
}