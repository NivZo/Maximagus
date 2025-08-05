using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Maximagus.Scripts.Spells.Implementations
{
    /// <summary>
    /// CLEANED UP: SpellProcessingManager with legacy event system removed
    /// No longer subscribes to CastSpellRequestedEvent - now called directly by PlayHandCommand
    /// </summary>
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

            // REMOVED: Legacy event subscription - replaced by direct method calls from PlayHandCommand
            // _eventBus.Subscribe<CastSpellRequestedEvent>(HandleCastSpellRequest);
        }

        // REMOVED: Legacy event handler - replaced by direct method calls
        // private void HandleCastSpellRequest(CastSpellRequestedEvent _)

        /// <summary>
        /// Process a spell using selected cards from the current GameState
        /// </summary>
        public void ProcessSpell()
        {
            GD.Print("--- Processing Spell (State-Driven) ---");
            var gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            var currentState = gameStateManager?.CurrentState;
            if (currentState == null)
            {
                GD.PrintErr("Cannot process spell - GameState is null");
                return;
            }

            // Get selected cards from state - no direct dependency on Card nodes
            var selectedCardStates = currentState.Hand.Cards
                .Where(c => currentState.Hand.SelectedCardIds.Contains(c.CardId))
                .ToArray();
            
            if (selectedCardStates.Length == 0)
            {
                GD.PrintErr("No selected cards found in state");
                return;
            }

            var context = new SpellContext();
            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            GD.Print($"Executing {selectedCardStates.Length} cards in the following order: {string.Join(", ", selectedCardStates.Select(c => c.CardName))}");

            // Find the corresponding visual cards for animation only
            var handManager = (HandManager)_handManager;
            Dictionary<string, Card> visualCards = new Dictionary<string, Card>();
            
            // Populate dictionary if possible
            if (handManager != null && handManager.Cards.Length > 0)
            {
                foreach (var card in handManager.Cards)
                {
                    if (card != null)
                    {
                        visualCards[card.GetInstanceId().ToString()] = card;
                    }
                }
            }

            foreach (var cardState in selectedCardStates)
            {
                GD.Print($"- Executing card: {cardState.CardName} (ID: {cardState.CardId})");
                
                // Try to find the visual card for animation purposes only
                visualCards.TryGetValue(cardState.CardId, out var visualCard);
                
                // Store card name for logging
                var cardName = cardState.CardName;
                
                _queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        // ANIMATION ONLY: If we have a visual card reference, animate it
                        if (visualCard?.Visual != null && !visualCard.Visual.IsQueuedForDeletion())
                        {
                            AnimationUtils.AnimateScale(visualCard.Visual, 1.5f, 1f, Tween.TransitionType.Elastic);
                        }
                        
                        // Execute the spell based on the visual card resource
                        // Note: In a fully state-driven system, we would load resources by ID
                        // But for now, we need to ensure we're getting the resource from the visualCard
                        
                        SpellCardResource resource = null;
                        
                        // Try to get the resource from the visual card
                        if (visualCard != null && visualCard.Resource != null)
                        {
                            resource = visualCard.Resource;
                            GD.Print($"Using resource from visual card: {resource.CardName}");
                        }
                        // If not found, try to get it from another card with matching ID
                        else if (!string.IsNullOrEmpty(cardState.ResourceId))
                        {
                            // Future improvement: ResourceManager.GetSpellCardResource(cardState.ResourceId)
                            GD.PrintErr($"Could not find visual card for: {cardName}");
                        }
                        
                        // Execute the card resource if we found it
                        if (resource != null)
                        {
                            GD.Print($"Executing card resource: {resource.CardName}");
                            resource.Execute(context);
                        }
                        else
                        {
                            GD.PrintErr($"ERROR: Could not find resource for card: {cardName}");
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
        
        /// <summary>
        /// Process spell with specific cards - temporary bridge method for backward compatibility
        /// </summary>
        public void ProcessSpellWithCards(global::Card[] cards)
        {
            GD.Print("--- Processing Spell With Direct Card References ---");
            
            if (cards == null || cards.Length == 0)
            {
                GD.PrintErr("No cards provided to ProcessSpellWithCards");
                return;
            }
            
            var context = new SpellContext();
            _statusEffectManager?.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            GD.Print($"Executing {cards.Length} cards in the following order: {string.Join(", ", cards.Select(c => c.Resource.CardName))}");

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
                            GD.Print($"Executing card resource: {cardResource.CardName}");
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
                GD.Print("--- Spell Finished (Direct Card Mode) ---");
            });
        }
    }
}
