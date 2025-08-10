using System;
using Scripts.State;
using System.Linq;
using Godot;

namespace Scripts.Commands.Card
{
    public class DeselectCardCommand : GameCommand
    {
        private readonly string _cardId;

        public DeselectCardCommand(string cardId) : base()
        {
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;

            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            var card = _commandProcessor.CurrentState.Cards.Cards.FirstOrDefault(c => c.CardId == _cardId);
            return card != null && card.ContainerType == ContainerType.Hand && card.IsSelected;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            var newCards = currentState.Cards.WithCardSelection(_cardId, false);
            var newState = currentState.WithCards(newCards);
            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return $"Deselect card: {_cardId}";
        }
    }
}