using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;
using Scripts.State;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private IStatusEffectManager _statusEffectManager;
        private IHandManager _handManager;
        private IEventBus _eventBus;
        private QueuedActionsManager _queuedActionsManager;

        public SpellProcessingManager()
        {
            _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
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
                
                // Store card data to avoid accessing disposed objects in queued actions
                var cardResource = card.Resource;
                var cardVisual = card.Visual;
                
                _queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        // Check if card visual is still valid before animating
                        if (cardVisual != null && !cardVisual.IsQueuedForDeletion())
                        {
                            AnimationUtils.AnimateScale(cardVisual, 1.5f, 1f, Tween.TransitionType.Elastic);
                        }
                        
                        // Execute the card resource (this should always be safe)
                        if (cardResource != null)
                        {
                            cardResource.Execute(context);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        GD.PrintErr($"Error executing card action: {ex.Message}");
                        // Continue with spell execution even if one card fails
                    }
                },
                delayAfter: .5f);
            }

            _queuedActionsManager.QueueAction(() =>
            {
                GD.Print($"Spell total damage dealt: {context.TotalDamageDealt}");
                GD.Print("--- Spell Finished ---");
                
                // No need to trigger game state event - spell processing is self-contained
            });
        }
    }
}
