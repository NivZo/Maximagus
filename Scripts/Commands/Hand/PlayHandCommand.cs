using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to play the currently selected cards as a spell
    /// </summary>
    public class PlayHandCommand : IGameCommand
    {
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
            Console.WriteLine("[PlayHandCommand] Execute() called!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[PlayHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            var selectedCards = handManager.Hand.SelectedCards;
            Console.WriteLine($"[PlayHandCommand] Playing {selectedCards.Length} selected cards");

            if (selectedCards.Length == 0)
            {
                Console.WriteLine("[PlayHandCommand] No cards selected!");
                return currentState;
            }

            // STEP 1: Process the spell using SpellProcessingManager (this queues spell animations)
            var spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
            if (spellProcessingManager != null)
            {
                Console.WriteLine("[PlayHandCommand] Processing spell...");
                spellProcessingManager.ProcessSpell(); // This queues spell animations and effects
                Console.WriteLine("[PlayHandCommand] Spell queued for processing");
            }
            else
            {
                Console.WriteLine("[PlayHandCommand] WARNING: SpellProcessingManager not available");
            }

            // STEP 2: Queue the discard and draw actions to happen after spell processing
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                Console.WriteLine("[PlayHandCommand] Queuing card discard and draw actions...");
                
                // Store selected cards for the queued action (to avoid stale references)
                var cardsToDiscard = selectedCards.ToArray();
                var cardCount = cardsToDiscard.Length;
                
                // Queue the discard and draw to happen after the spell animations complete
                queuedActionsManager.QueueAction(() =>
                {
                    Console.WriteLine("[PlayHandCommand] Executing queued discard and draw...");
                    handManager.Hand.Discard(cardsToDiscard);
                    handManager.Hand.DrawAndAppend(cardCount);
                    Console.WriteLine("[PlayHandCommand] Cards discarded and replaced successfully");
                });
            }
            else
            {
                Console.WriteLine("[PlayHandCommand] WARNING: QueuedActionsManager not available, executing immediately");
                // Fallback to immediate execution if QueuedActionsManager is not available
                handManager.Hand.Discard(selectedCards);
                handManager.Hand.DrawAndAppend(selectedCards.Length);
                Console.WriteLine("[PlayHandCommand] Cards played and replaced successfully");
            }

            // STEP 3: Update GameState - transition to SpellCasting phase and update player
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.SpellCasting);
            var newPlayerState = currentState.Player.WithHandUsed();
            
            // Create new state with updated phase and player
            var newState = currentState
                .WithPhase(newPhaseState)
                .WithPlayer(newPlayerState);
            
            Console.WriteLine($"[PlayHandCommand] State updated: {newState.Phase.CurrentPhase} phase, hands remaining: {newState.Player.RemainingHands}");
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