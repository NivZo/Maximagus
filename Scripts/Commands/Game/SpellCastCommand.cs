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

        public override void Execute(CommandCompletionToken token)
        {

            var currentState = _commandProcessor.CurrentState;
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnEnd);
            var newState = currentState
                .WithPhase(newPhaseState);

            var followUpCommands = new[] { new TurnEndCommand() };
            var callback = () => token.Complete(CommandResult.Success(newState, followUpCommands));
            _spellProcessingManager.ProcessSpell(callback);
        }

        public override string GetDescription()
        {
            return "Process spell casting";
        }
    }
}