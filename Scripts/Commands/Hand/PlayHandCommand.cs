using System;
using System.Linq;
using Scripts.State;
using Scripts.Commands.Game;
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

            // Store the specific cards to discard by their instance IDs (to avoid issues with selection changes)
            var cardIdsToDiscard = selectedCards.Select(card => card.GetInstanceId()).ToArray();
            var cardCount = selectedCards.Length;

            // STEP 1: Process the spell using SpellProcessingManager 
            var spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
            if (spellProcessingManager != null)
            {
                _logger?.LogInfo("[PlayHandCommand] Processing spell...");
                spellProcessingManager.ProcessSpell(); // This queues spell animations and effects
                _logger?.LogInfo("[PlayHandCommand] Spell queued for processing");
            }
            else
            {
                _logger?.LogWarning("[PlayHandCommand] WARNING: SpellProcessingManager not available");
                return currentState;
            }

            // STEP 2: Queue discard of the originally selected cards after spell completes
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                _logger?.LogInfo("[PlayHandCommand] Queuing discard of originally selected cards...");
                
                // Queue the discard to happen after spell processing
                queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        _logger?.LogInfo("[PlayHandCommand] Executing discard of originally selected cards...");
                        
                        // Get fresh reference to HandManager
                        var handManagerForDiscard = ServiceLocator.GetService<IHandManager>();
                        if (handManagerForDiscard?.Hand == null)
                        {
                            _logger?.LogError("[PlayHandCommand] ERROR: HandManager not available for discard");
                            return;
                        }

                        // Find the originally selected cards by their instance IDs
                        var currentCards = handManagerForDiscard.Hand.Cards;
                        var cardsToDiscard = currentCards
                            .Where(card => cardIdsToDiscard.Contains(card.GetInstanceId()))
                            .ToArray();

                        if (cardsToDiscard.Length > 0)
                        {
                            _logger?.LogInfo($"[PlayHandCommand] Discarding {cardsToDiscard.Length} originally selected cards");
                            handManagerForDiscard.Hand.Discard(cardsToDiscard);
                            _logger?.LogInfo("[PlayHandCommand] Cards discarded successfully");
                        }
                        else
                        {
                            _logger?.LogWarning("[PlayHandCommand] WARNING: No originally selected cards found to discard");
                        }
                        
                        // Queue turn start after successful discard
                        _logger?.LogInfo("[PlayHandCommand] Queuing turn start...");
                        queuedActionsManager.QueueAction(() =>
                        {
                            try
                            {
                                var gameCommandProcessor = ServiceLocator.GetService<Scripts.Commands.GameCommandProcessor>();
                                if (gameCommandProcessor != null)
                                {
                                    var turnStartCommand = new TurnStartCommand();
                                    var success = gameCommandProcessor.ExecuteCommand(turnStartCommand);
                                    _logger?.LogInfo($"[PlayHandCommand] Turn start executed: {success}");
                                }
                                else
                                {
                                    _logger?.LogError("[PlayHandCommand] ERROR: GameCommandProcessor not available");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError($"[PlayHandCommand] ERROR in turn start: {ex.Message}", ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[PlayHandCommand] ERROR in discard: {ex.Message}", ex);
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[PlayHandCommand] WARNING: QueuedActionsManager not available");
            }

            // STEP 3: Update GameState - stay in CardSelection phase, just update player
            var newPlayerState = currentState.Player.WithHandUsed();
            var newState = currentState.WithPlayer(newPlayerState);
            
            _logger?.LogInfo($"[PlayHandCommand] State updated: hands remaining: {newState.Player.RemainingHands}");
            return newState;
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // For undo, we need to restore the previous hand and player state
            // This is a complex undo that requires restoring multiple components
            return new RestoreGameStateCommand(previousState);
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

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            return new RestoreGameStateCommand(previousState);
        }

        public string GetDescription()
        {
            return $"Restore game state to: {_targetState?.Phase?.CurrentPhase}";
        }
    }
}