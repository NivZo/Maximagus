using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Maximagus.Scripts.Managers;
using Scripts.Commands;

namespace Maximagus.Scripts.Spells.Implementations
{
    /// <summary>
    /// CLEANED UP: SpellProcessingManager with legacy event system removed
    /// No longer subscribes to CastSpellRequestedEvent - now called directly by PlayHandCommand
    /// </summary>
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private ILogger _logger;
        private IGameCommandProcessor _commandProcessor;
        private IStatusEffectManager _statusEffectManager;

        public SpellProcessingManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
        }

        public void ProcessSpell()
        {
            _logger.LogInfo("--- Processing Spell ---");
            var currentState = _commandProcessor.CurrentState;


            // Get selected cards from state - no direct dependency on Card nodes
            var selectedCardStates = currentState.Hand.SelectedCards;

            if (selectedCardStates.Count() == 0)
            {
                _logger.LogError("No selected cards found in state");
                return;
            }

            var context = new SpellContext();
            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            _logger.LogInfo($"Executing {selectedCardStates.Count()} cards in the following order: {string.Join(", ", selectedCardStates.Select(c => c.Resource.CardName))}");


            foreach (var cardState in selectedCardStates)
            {
                _logger.LogInfo($"- Executing card: {cardState.Resource.CardName} (ID: {cardState.CardId})");
                cardState.Resource.Execute(context);
            }

            _logger.LogInfo($"Spell total damage dealt: {context.TotalDamageDealt}");
            _logger.LogInfo("--- Spell Finished ---");
        }
    }
}
