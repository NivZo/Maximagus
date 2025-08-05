using System;
using System.Linq;
using Scripts.State;

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
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                _logger?.LogError("[PlayHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            var selectedCards = handManager.Hand.SelectedCards;
            _logger?.LogInfo($"[PlayHandCommand] Playing {selectedCards.Length} selected cards");

            if (selectedCards.Length == 0)
            {
                _logger?.LogWarning("[PlayHandCommand] No cards selected!");
                return currentState;
            }

            // STEP 1: Process the spell using SpellProcessingManager (this queues animations)
            var spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
            if (spellProcessingManager != null)
            {
                _logger?.LogInfo("[PlayHandCommand] Processing spell...");
                spellProcessingManager.ProcessSpell(); // This queues spell animations
                _logger?.LogInfo("[PlayHandCommand] Spell queued for processing");
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
                var cardCount = selectedCards.Length;
                var cardsToDiscard = selectedCards.ToArray(); // Store reference
                
                _logger?.LogInfo("[PlayHandCommand] Queuing discard and replace after spell animations...");
                
                // Queue with delay to happen after spell processing completes
                queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        _logger?.LogInfo("[PlayHandCommand] Executing delayed discard and replace...");
                        
                        var handManagerForActions = ServiceLocator.GetService<IHandManager>();
                        if (handManagerForActions?.Hand != null)
                        {
                            // Check if cards still exist before discarding
                            var existingCards = handManagerForActions.Hand.Cards;
                            var validCardsToDiscard = cardsToDiscard
                                .Where(card => existingCards.Contains(card))
                                .ToArray();
                            
                            if (validCardsToDiscard.Length > 0)
                            {
                                handManagerForActions.Hand.Discard(validCardsToDiscard);
                                _logger?.LogInfo($"[PlayHandCommand] {validCardsToDiscard.Length} cards discarded");
                            }
                            
                            // Draw replacement cards
                            var currentCount = handManagerForActions.Hand.Cards.Length;
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
                    handManager.Hand.Discard(selectedCards);
                    handManager.Hand.DrawAndAppend(selectedCards.Length);
                    _logger?.LogInfo("[PlayHandCommand] Immediate discard and replace completed");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"[PlayHandCommand] ERROR in immediate discard and replace: {ex.Message}", ex);
                }
            }

            // STEP 3: Update GameState - stay in CardSelection phase, just update player
            var newPlayerState = currentState.Player.WithHandUsed();
            var newState = currentState.WithPlayer(newPlayerState);
            
            _logger?.LogInfo($"[PlayHandCommand] State updated: hands remaining: {newState.Player.RemainingHands}");
            return newState;
        }

        public string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }

    /// <summary>
    /// Command to restore the complete game state (used for complex undos)
    /// </summary>
    public class RestoreGameStateCommand : IGameCommand
    {
        private readonly IGameStateData _targetState;

        public RestoreGameStateCommand(IGameStateData targetState)
        {
            _targetState = targetState ?? throw new ArgumentNullException(nameof(targetState));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            // Can always restore to a valid previous state
            return _targetState?.IsValid() == true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute RestoreGameStateCommand - target state is invalid");

            return _targetState;
        }

        public string GetDescription()
        {
            return $"Restore game state to: {_targetState?.Phase?.CurrentPhase}";
        }
    }
}