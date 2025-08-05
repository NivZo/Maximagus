using System;
using System.Linq;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to play the currently selected cards as a spell
    /// </summary>
    public class PlayHandCommand : IGameCommand
    {
        private readonly ILogger _logger;

        public PlayHandCommand()
        {
            _logger = ServiceLocator.GetService<ILogger>();
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Must be in card selection phase (where player can play cards)
            if (!currentState.Phase.AllowsCardSelection) return false;

            // Player must have hands remaining
            if (!currentState.Player.HasHandsRemaining) return false;

            // Must have at least one card selected
            if (currentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            _logger?.LogInfo("[PlayHandCommand] Execute() called!");
            
            // PURE COMMAND SYSTEM: Get selected cards from GameState, not visual components
            var selectedCardIds = currentState.Hand.SelectedCardIds.ToList();
            var selectedCardStates = currentState.Hand.Cards.Where(card => selectedCardIds.Contains(card.CardId)).ToList();
            
            _logger?.LogInfo($"[PlayHandCommand] Playing {selectedCardStates.Count} selected cards from GameState");

            if (selectedCardStates.Count == 0)
            {
                _logger?.LogWarning("[PlayHandCommand] No cards selected in GameState!");
                return currentState;
            }

            // Get HandManager to access the Hand for visual operations (spell processing, discard)
            var handManager = ServiceLocator.GetService<IHandManager>() as HandManager;
            if (handManager?.Hand == null)
            {
                _logger?.LogError("[PlayHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Get the actual Card objects by matching IDs from GameState to visual cards
            var selectedVisualCards = handManager.Cards.Where(card =>
                selectedCardIds.Contains(card.GetInstanceId().ToString())).ToArray();
            
            _logger?.LogInfo($"[PlayHandCommand] Found {selectedVisualCards.Length} matching visual cards");

            if (selectedVisualCards.Length == 0)
            {
                _logger?.LogWarning("[PlayHandCommand] No matching visual cards found!");
                return currentState;
            }

            // STEP 1: Process the spell with the selected cards
            var spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
            if (spellProcessingManager != null)
            {
                _logger?.LogInfo("[PlayHandCommand] Processing spell with selected cards...");
                
                // Pass the visual cards directly to ensure spell processing works correctly
                // This provides a smooth transition to state-driven processing
                if (selectedVisualCards.Length > 0)
                {
                    ((SpellProcessingManager)spellProcessingManager).ProcessSpellWithCards(selectedVisualCards);
                    _logger?.LogInfo("[PlayHandCommand] Spell processing completed");
                }
                else
                {
                    _logger?.LogWarning("[PlayHandCommand] No visual cards to process");
                }
            }
            else
            {
                _logger?.LogWarning("[PlayHandCommand] WARNING: SpellProcessingManager not available");
                return currentState;
            }

            // STEP 2: Queue discard and replace to happen AFTER spell animations
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                var cardCount = selectedVisualCards.Length;
                var cardsToDiscard = selectedVisualCards.ToArray(); // Store reference
                
                _logger?.LogInfo("[PlayHandCommand] Queuing discard and replace after spell animations...");
                
                // Queue with delay to happen after spell processing completes
                queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        _logger?.LogInfo("[PlayHandCommand] Executing delayed discard and replace...");
                        
                        var handManagerForActions = ServiceLocator.GetService<IHandManager>() as HandManager;
                        if (handManagerForActions?.Hand != null)
                        {
                            // Check if cards still exist before discarding
                            var existingCards = handManagerForActions.Cards;
                            var validCardsToDiscard = cardsToDiscard
                                .Where(card => existingCards.Contains(card))
                                .ToArray();
                            
                            if (validCardsToDiscard.Length > 0)
                            {
                                handManagerForActions.Hand.Discard(validCardsToDiscard);
                                _logger?.LogInfo($"[PlayHandCommand] {validCardsToDiscard.Length} cards discarded");
                            }
                            
                            // Draw replacement cards
                            var currentCount = handManagerForActions.Cards.Length;
                            var maxHandSize = 10;
                            var cardsToDraw = Math.Max(0, maxHandSize - currentCount);
                            
                            if (cardsToDraw > 0)
                            {
                                handManagerForActions.Hand.DrawAndAppend(cardsToDraw);
                                _logger?.LogInfo($"[PlayHandCommand] {cardsToDraw} replacement cards drawn");
                            }
                            
                            _logger?.LogInfo("[PlayHandCommand] Discard and replace completed successfully");
                        }
                        else
                        {
                            _logger?.LogError("[PlayHandCommand] ERROR: HandManager not available for actions");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[PlayHandCommand] ERROR in delayed discard and replace: {ex.Message}", ex);
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[PlayHandCommand] WARNING: QueuedActionsManager not available - executing immediately");
                // Fallback: immediate execution (will cause visual issues but spell will work)
                try
                {
                    handManager.Hand.Discard(selectedVisualCards);
                    handManager.Hand.DrawAndAppend(selectedVisualCards.Length);
                    _logger?.LogInfo("[PlayHandCommand] Immediate discard and replace completed");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"[PlayHandCommand] ERROR in immediate discard and replace: {ex.Message}", ex);
                }
            }

            // STEP 3: Update GameState - clear selected cards after playing and update player
            var newHandState = currentState.Hand.WithClearedSelection();
            var newPlayerState = currentState.Player.WithHandUsed();
            var newState = currentState.WithHand(newHandState).WithPlayer(newPlayerState);
            
            _logger?.LogInfo($"[PlayHandCommand] State updated: hands remaining: {newState.Player.RemainingHands}, selected cards cleared");
            return newState;
        }

        // Removed ProcessSpellWithCards method - now using SpellProcessingManager which works with GameState directly

        public string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }
}