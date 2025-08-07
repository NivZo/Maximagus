using System;
using System.Linq;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Game;
using Godot;
using Maximagus.Scripts.Enums;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// PURE COMMAND: Initiates spell casting by transitioning to SpellCasting phase
    /// Natural phase flow: CardSelection -> SpellCasting (on play) -> TurnEnd -> TurnStart
    /// </summary>
    public class PlayHandCommand : GameCommand
    {
        public PlayHandCommand() : base()
        {
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;

            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;

            if (!_commandProcessor.CurrentState.Player.HasHandsRemaining) return false;

            if (_commandProcessor.CurrentState.Hand.SelectedCount == 0) return false;

            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            return true;
        }

        public override CommandResult ExecuteWithResult()
        {
            _logger.LogInfo("[PlayHandCommand] Initiating spell casting with command result...");

            // Pure state computation - no side effects
            var newPlayerState = _commandProcessor.CurrentState.Player.WithHandUsed();
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.SpellCasting);
            var newState = _commandProcessor.CurrentState
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState);

            _logger.LogInfo($"[PlayHandCommand] Transitioned to SpellCasting phase. Hands remaining: {newPlayerState.RemainingHands}");

            // Follow-up command to handle the actual spell processing
            var followUpCommands = new[] { new SpellCastCommand() };

            return CommandResult.Success(newState, followUpCommands);
        }

        public override string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }
}