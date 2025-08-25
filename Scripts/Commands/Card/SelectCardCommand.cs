using System;
using Scripts.State;
using System.Linq;
using Godot;

namespace Scripts.Commands.Card
{
    /// <summary>
    /// PURE COMMAND SYSTEM: Command to select a card in the player's hand
    /// Updates only GameState - visual sync happens automatically
    /// </summary>
    public class SelectCardCommand : GameCommand
    {
        private readonly string _cardId;

        public SelectCardCommand(string cardId) : base()
        {
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;
            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            var card = _commandProcessor.CurrentState.Cards.Cards.FirstOrDefault(c => c.CardId == _cardId);
            if (card == null) return false;
            if (card.ContainerType != ContainerType.Hand) return false;
            if (card.IsSelected) return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            _logger.LogInfo($"[SelectCardCommand] Selecting card {_cardId} in GameState");

            var newCards = currentState.Cards.WithCardSelection(_cardId, true);
            var newState = currentState.WithCards(newCards);

            _logger.LogInfo($"[SelectCardCommand] Card {_cardId} selected in GameState successfully");

            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return $"Select card: {_cardId}";
        }
    }
}