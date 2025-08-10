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

            foreach (var card in _commandProcessor.CurrentState.Hand.Cards)
            {
                if (card.CardId == _cardId)
                {
                    return card.IsSelected;
                }
            }

            return false;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            var newHandState = currentState.Hand.WithCardSelection(_cardId, false);
            var newState = currentState.WithHand(newHandState);
            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return $"Deselect card: {_cardId}";
        }
    }
}