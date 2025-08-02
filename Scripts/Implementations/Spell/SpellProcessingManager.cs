
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private IStatusEffectManager _statusEffectManager;
        private IGameStateManager _gameStateManager;

        public SpellProcessingManager()
        {
            _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
            _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
        }

        public void ProcessSpell(Array<SpellCardResource> cards)
        {
            GD.Print("--- Processing Spell ---");
            var context = new SpellContext();

            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.CardName))}");

            foreach (var card in cards)
            {
                GD.Print($"- Executing card: {card.CardName}");
                card.Execute(context);
            }

            GD.Print($"Spell total damage dealt: {context.TotalDamageDealt}");
            GD.Print("--- Spell Finished ---");

            _gameStateManager.TriggerEvent(GameStateEvent.SpellsComplete);
        }
    }
}
