using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to discard the currently selected cards without playing them
    /// </summary>
    public class DiscardHandCommand : IGameCommand
    {
        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Can discard during card selection phase
            if (!currentState.Phase.AllowsCardSelection)
                return false;

            // Must have at least one card selected
            if (currentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine("[DiscardHandCommand] Execute() called!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[DiscardHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            var selectedCards = handManager.Hand.SelectedCards;
            Console.WriteLine($"[DiscardHandCommand] Discarding {selectedCards.Length} selected cards");

            if (selectedCards.Length == 0)
            {
                Console.WriteLine("[DiscardHandCommand] No cards selected!");
                return currentState;
            }

            // Queue only the discard action - cards will be drawn at turn start
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                Console.WriteLine("[DiscardHandCommand] Queuing card discard action...");
                
                // Store selected cards for the queued action (to avoid stale references)
                var cardsToDiscard = selectedCards.ToArray();
                
                // Queue only the discard
                queuedActionsManager.QueueAction(() =>
                {
                    Console.WriteLine("[DiscardHandCommand] Executing queued discard...");
                    handManager.Hand.Discard(cardsToDiscard);
                    Console.WriteLine("[DiscardHandCommand] Cards discarded successfully (will draw at turn start)");
                });
            }
            else
            {
                Console.WriteLine("[DiscardHandCommand] WARNING: QueuedActionsManager not available, executing immediately");
                // Fallback to immediate execution if QueuedActionsManager is not available
                handManager.Hand.Discard(selectedCards);
                Console.WriteLine("[DiscardHandCommand] Cards discarded successfully");
            }

            // Discard loops back to CardSelection phase (following the turn loop)
            var newPhaseState = currentState.Phase.WithPhase(currentState.Phase.GetDiscardPhase());
            return currentState.WithPhase(newPhaseState);
        }

        public string GetDescription()
        {
            return "Discard selected cards";
        }
    }
}