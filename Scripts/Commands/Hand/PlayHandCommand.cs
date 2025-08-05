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

            // STEP 1: Immediately discard selected cards before spell processing
            _logger?.LogInfo("[PlayHandCommand] Discarding selected cards immediately...");
            try
            {
                handManager.Hand.Discard(selectedCards);
                _logger?.LogInfo($"[PlayHandCommand] {selectedCards.Length} cards discarded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[PlayHandCommand] ERROR discarding cards: {ex.Message}", ex);
                return currentState;
            }

            // STEP 2: Process the spell using SpellProcessingManager 
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
            }

            // STEP 3: Queue card drawing to happen after spell processing
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                var cardCount = selectedCards.Length;
                _logger?.LogInfo($"[PlayHandCommand] Queuing {cardCount} replacement cards...");
                
                queuedActionsManager.QueueAction(() =>
                {
                    try
                    {
                        _logger?.LogInfo("[PlayHandCommand] Drawing replacement cards...");
                        
                        var handManagerForDraw = ServiceLocator.GetService<IHandManager>();
                        if (handManagerForDraw?.Hand != null)
                        {
                            handManagerForDraw.Hand.DrawAndAppend(cardCount);
                            _logger?.LogInfo($"[PlayHandCommand] {cardCount} replacement cards drawn successfully");
                        }
                        else
                        {
                            _logger?.LogError("[PlayHandCommand] ERROR: HandManager not available for drawing");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[PlayHandCommand] ERROR drawing replacement cards: {ex.Message}", ex);
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[PlayHandCommand] WARNING: QueuedActionsManager not available");
            }

            // STEP 4: Update GameState - stay in CardSelection phase, just update player
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