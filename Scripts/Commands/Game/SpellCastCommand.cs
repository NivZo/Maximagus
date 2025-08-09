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
            return _commandProcessor.CurrentState?.Phase?.CurrentPhase == GamePhase.SpellCasting;
        }

        public override CommandResult ExecuteWithResult()
        {
            _spellProcessingManager.ProcessSpell();

            var currentState = _commandProcessor.CurrentState;
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnEnd);
            var newState = currentState
                .WithPhase(newPhaseState);

            var followUpCommands = new[] { new TurnEndCommand() };

            (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(5).Timeout += _commandProcessor.NotifyBlockingCommandFinished;

            return CommandResult.Success(newState, followUpCommands);
        }

        public override string GetDescription()
        {
            return "Process spell casting";
        }
    }
}