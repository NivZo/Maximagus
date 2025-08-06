using System;
using System.Linq;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;
using Maximagus.Scripts.Managers;
using Scripts.Commands.Game;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to play the currently selected cards as a spell
    /// </summary>
    public class PlayHandCommand : GameCommand
    {
        private readonly ISpellProcessingManager _spellProcessingManager;

        public PlayHandCommand() : base()
        {
            _spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
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

        public override IGameStateData Execute()
        {
            _logger.LogInfo("[PlayHandCommand] Execute() called!");

            var newPlayerState = _commandProcessor.CurrentState.Player.WithHandUsed();
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.SpellCasting);
            var newState = _commandProcessor.CurrentState
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState);
            _commandProcessor.SetState(newState);

            _spellProcessingManager.ProcessSpell();

            var newHandState = newState.Hand.WithRemovedCards(_commandProcessor.CurrentState.Hand.SelectedCards.Select(c => c.CardId));
            newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.TurnEnd);
            newState = newState
                .WithHand(newHandState)
                .WithPhase(newPhaseState);
            _commandProcessor.SetState(newState);
            _logger.LogInfo($"[PlayHandCommand] State updated: hands remaining: {newState.Player.RemainingHands}, cards remaining: {newHandState.Count}");

            var command = new TurnStartCommand();
            _commandProcessor.ExecuteCommand(command);

            return _commandProcessor.CurrentState;
        }

        public override string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }
}