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

            // Can only select cards during phases that allow player action
            if (!_commandProcessor.CurrentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            // Card must exist in hand
            var cardExists = false;
            foreach (var card in _commandProcessor.CurrentState.Hand.Cards)
            {
                if (card.CardId == _cardId)
                {
                    cardExists = true;
                    // Card must not already be selected
                    if (card.IsSelected) return false;
                    break;
                }
            }

            return cardExists;
        }

        public override IGameStateData Execute()
        {
            GD.Print($"[SelectCardCommand] Selecting card {_cardId} in GameState");
            
            // PURE COMMAND SYSTEM: Update only GameState
            // CardLogic.SyncWithGameState() will handle visual updates automatically
            var newHandState = _commandProcessor.CurrentState.Hand.WithCardSelection(_cardId, true);
            var newState = _commandProcessor.CurrentState.WithHand(newHandState);

            GD.Print($"[SelectCardCommand] Card {_cardId} selected in GameState successfully");
            return newState;
        }

        public override string GetDescription()
        {
            return $"Select card: {_cardId}";
        }
    }
}