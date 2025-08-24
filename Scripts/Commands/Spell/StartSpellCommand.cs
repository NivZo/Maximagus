using System;
using Scripts.State;
using Godot;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to initialize spell state when spell casting begins.
    /// Sets up the active spell state with empty properties and modifiers.
    /// </summary>
    public class StartSpellCommand : GameCommand
    {
        public StartSpellCommand() : base(false)
        {
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Cannot start a spell if one is already active
            if (currentState.Spell.IsActive)
            {
                _logger.LogWarning("[StartSpellCommand] Cannot start spell - spell already active");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            GD.Print("[StartSpellCommand] Starting new spell");
            
            var newSpellState = currentState.Spell.WithActiveSpell(DateTime.UtcNow);
            var newState = currentState.WithSpell(newSpellState);
            
            GD.Print($"[StartSpellCommand] Spell started at {newSpellState.StartTime}");
            
            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return "Start spell casting";
        }
    }
}