using System;
using System.Linq;
using Scripts.State;
using Scripts.Commands;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to handle the spell casting phase - processes the spell effects
    /// Enters SpellCasting phase, processes spell, and naturally transitions to TurnEnd
    /// </summary>
    public class SpellCastCommand : GameCommand
    {
        private readonly ISpellProcessingManager _spellProcessingManager;

        public SpellCastCommand() : base(true)
        {
            _spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
        }

        public override bool CanExecute()
        {
            // Can only cast spell when we have selected cards and are in the right phase
            return _commandProcessor.CurrentState?.Phase?.CurrentPhase == GamePhase.SpellCasting &&
                   _commandProcessor.CurrentState?.Hand?.SelectedCount > 0;
        }

        public override CommandResult ExecuteWithResult()
        {
            var currentState = _commandProcessor.CurrentState;
            _logger.LogInfo("[SpellCastCommand] Processing spell with command result...");

            // Process the spell using the selected cards
            _spellProcessingManager.ProcessSpell();

            // Remove the selected cards from hand
            var selectedCardIds = currentState.Hand.SelectedCards.Select(c => c.CardId);
            var newHandState = currentState.Hand.WithRemovedCards(selectedCardIds);

            // Transition to TurnEnd phase
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnEnd);
            var newState = currentState
                .WithHand(newHandState)
                .WithPhase(newPhaseState);

            _logger.LogInfo($"[SpellCastCommand] Spell processed, transitioning to TurnEnd. Cards remaining: {newHandState.Count}");

            // Create follow-up command to continue the turn flow
            var followUpCommands = new[] { new TurnStartCommand() };

            (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(3).Timeout += _commandProcessor.NotifyBlockingCommandFinished;

            return CommandResult.Success(newState, followUpCommands);
        }

        public override string GetDescription()
        {
            return "Process spell casting";
        }
    }
}