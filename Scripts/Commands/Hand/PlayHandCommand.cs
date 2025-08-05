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

            // Store card count for replacement later
            var cardCount = selectedCards.Length;

            // STEP 1: Process the spell using SpellProcessingManager 
            // NOTE: SpellProcessingManager might handle discard internally, so we need to check after
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

            // STEP 2: Queue card replacement to happen after spell processing
            // We'll check if cards still exist before trying to discard them
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                _logger?.LogInfo("[PlayHandCommand] Queuing card replacement after spell...");
                
                // Queue the replacement logic to happen after spell processing
                queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        _logger?.LogInfo("[PlayHandCommand] Executing card replacement...");
                        
                        // Get fresh reference to HandManager
                        var handManagerForReplacement = ServiceLocator.GetService<IHandManager>();
                        if (handManagerForReplacement?.Hand == null)
                        {
                            _logger?.LogError("[PlayHandCommand] ERROR: HandManager not available for replacement");
                            return;
                        }

                        // Check current hand size to see if we need to draw cards
                        var currentCardCount = handManagerForReplacement.Hand.Cards.Length;
                        var maxHandSize = 10;
                        var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                        
                        if (cardsToDraw > 0)
                        {
                            _logger?.LogInfo($"[PlayHandCommand] Drawing {cardsToDraw} cards to replace discarded ones");
                            handManagerForReplacement.Hand.DrawAndAppend(cardsToDraw);
                            _logger?.LogInfo($"[PlayHandCommand] Hand now has {handManagerForReplacement.Hand.Cards.Length} cards");
                        }
                        else
                        {
                            _logger?.LogInfo("[PlayHandCommand] Hand already at max size, no replacement needed");
                        }
                        
                        _logger?.LogInfo("[PlayHandCommand] Card replacement completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[PlayHandCommand] ERROR in card replacement: {ex.Message}", ex);
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