using System;
using Scripts.State;
using System.Linq;
using Godot;

namespace Scripts.Commands.Card
{
    /// <summary>
    /// PURE COMMAND SYSTEM: Command to deselect a card in the player's hand
    /// Updates only GameState - visual sync happens automatically
    /// </summary>
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

            // Can only deselect cards during phases that allow player action
            if (!_commandProcessor.CurrentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            // Card must exist in hand and be selected
            foreach (var card in _commandProcessor.CurrentState.Hand.Cards)
            {
                if (card.CardId == _cardId)
                {
                    return card.IsSelected; // Can only deselect if currently selected
                }
            }

            return false; // Card not found
        }

        public override IGameStateData Execute()
        {
            GD.Print($"[DeselectCardCommand] Deselecting card {_cardId} in GameState");
            
            // PURE COMMAND SYSTEM: Update only GameState
            // CardLogic.SyncWithGameState() will handle visual updates automatically
            var newHandState = _commandProcessor.CurrentState.Hand.WithCardSelection(_cardId, false);
            var newState = _commandProcessor.CurrentState.WithHand(newHandState);

            GD.Print($"[DeselectCardCommand] Card {_cardId} deselected in GameState successfully");
            return newState;
        }

        public override string GetDescription()
        {
            return $"Deselect card: {_cardId}";
        }
    }
}