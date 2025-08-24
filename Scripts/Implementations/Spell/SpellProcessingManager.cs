using Godot;
using System.Linq;
using Scripts.Commands;
using Scripts.Commands.Game;
using System;
using Scripts.State;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private ILogger _logger;
        private IGameCommandProcessor _commandProcessor;

        public SpellProcessingManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public void ProcessSpell(Action onFinishCallback)
        {
            var currentState = _commandProcessor.CurrentState;
        
            // Validate that we have played cards to process
            var playedCardStates = currentState.Cards
                .PlayedCards?
                .OrderBy(c => c.Position)
                .ToArray();
        
            if (playedCardStates == null || playedCardStates.Length == 0)
            {
                _logger.LogError("No played cards found in state");
                onFinishCallback?.Invoke();
                return;
            }

            // Use the new command-based spell processing
            // SpellCastCommand handles the entire spell processing chain
            var spellCastCommand = new SpellCastCommand();
            
            if (!spellCastCommand.CanExecute())
            {
                _logger.LogError("Cannot execute spell cast command");
                onFinishCallback?.Invoke();
                return;
            }

            // Execute the spell cast command which handles the full command chain
            bool success = _commandProcessor.ExecuteCommand(spellCastCommand);
            
            if (success)
            {
                _logger.LogInfo("Spell processing completed successfully");
            }
            else
            {
                _logger.LogError("Spell processing failed");
            }
            
            // Since commands are now synchronous, we can call the callback immediately
            onFinishCallback?.Invoke();
        }
    }
}
