using System;
using System.Linq;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Game;
using Godot;
using Maximagus.Scripts.Enums;

namespace Scripts.Commands.Hand
{

    public class PlayHandCommand : GameCommand
    {
        public PlayHandCommand() : base()
        {
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState.Phase.CurrentPhase != GamePhase.CardSelection) return false;

            if (_commandProcessor.CurrentState == null) return false;

            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;

            if (!_commandProcessor.CurrentState.Player.HasHandsRemaining) return false;

            // Require at least one selected card in Hand
            if (!_commandProcessor.CurrentState.Cards.SelectedInHand.Any()) return false;

            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            _logger.LogInfo("[PlayHandCommand] Initiating spell casting with command result...");

            var currentState = _commandProcessor.CurrentState;

            // Move selected Hand cards to PlayedCards and clear selection in hand
            var selectedIds = currentState.Cards.SelectedInHand.Select(c => c.CardId).ToArray();
            var moved = currentState.Cards.WithMovedToContainer(selectedIds, ContainerType.PlayedCards);
            var newCards = moved.WithClearedSelection().WithClearedHover();

            var newPlayerState = currentState.Player.WithHandUsed();
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.SpellCasting);
            var newState = currentState
                .WithCards(newCards)
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState);

            _logger.LogInfo($"[PlayHandCommand] Transitioned to SpellCasting phase. Hands remaining: {newPlayerState.RemainingHands}");

            var followUpCommands = new[] { new SpellCastCommand() };
            token.Complete(CommandResult.Success(newState, followUpCommands));
        }

        public override string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }
}