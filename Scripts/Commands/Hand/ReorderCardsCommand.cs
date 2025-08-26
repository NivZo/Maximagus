using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Hand
{

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
            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            var currentCardIds = _commandProcessor.CurrentState.Cards.HandCards.Select(c => c.CardId).ToHashSet();
            var newOrderSet = _newCardOrder.ToHashSet();

            if (!newOrderSet.IsSubsetOf(currentCardIds))
                return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;

            // Use centralized CardsState to apply the new order to hand cards
            var newCards = currentState.Cards.WithReorderedHandCards(_newCardOrder);
            var newState = currentState.WithCards(newCards);

            token.Complete(CommandResult.Success(newState));
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