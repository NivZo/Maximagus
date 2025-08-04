
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private IStatusEffectManager _statusEffectManager;
        private IGameStateManager _gameStateManager;
        private IHandManager _handManager;
        private IEventBus _eventBus;
        private QueuedActionsManager _queuedActionsManager;

        public SpellProcessingManager()
        {
            _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
            _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            _handManager = ServiceLocator.GetService<IHandManager>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();

            _eventBus.Subscribe<CastSpellRequestedEvent>(HandleCastSpellRequest);
        }

        private void HandleCastSpellRequest(CastSpellRequestedEvent _)
        {
            ProcessSpell();
        }

        public void ProcessSpell()
        {
            GD.Print("--- Processing Spell ---");
            var cards = _handManager.Hand.SelectedCards.ToArray();
            var context = new SpellContext();

            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.Resource.CardName))}");

            foreach (var card in cards)
            {
                GD.Print($"- Executing card: {card.Resource.CardName}");
                _queuedActionsManager.QueueAction(() =>
                {
                    AnimationUtils.AnimateScale(card.Visual, 1.5f, 1f, Tween.TransitionType.Elastic);
                    card.Resource.Execute(context);
                },
                delayAfter: .5f);
            }

            _queuedActionsManager.QueueAction(() =>
            {
                GD.Print($"Spell total damage dealt: {context.TotalDamageDealt}");
                GD.Print("--- Spell Finished ---");

                _gameStateManager.TriggerEvent(GameStateEvent.SpellsComplete);
            });
        }
    }
}
