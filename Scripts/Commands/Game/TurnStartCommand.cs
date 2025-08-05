using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to handle turn start - draws cards to max hand size and transitions to CardSelection
    /// </summary>
    public class TurnStartCommand : IGameCommand
    {
        public string GetDescription() => "Start Turn";

        public bool CanExecute(IGameStateData currentState)
        {
            // Can only start turn from TurnStart phase
            return currentState.Phase.CurrentPhase == GamePhase.TurnStart;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine("[TurnStartCommand] Execute() called!");
            Console.WriteLine($"[TurnStartCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[TurnStartCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Queue the draw action to fill hand to maximum size
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                Console.WriteLine("[TurnStartCommand] Queuing card draw to max hand size...");
                
                queuedActionsManager.QueueAction(() =>
                {
                    var currentCardCount = handManager.Hand.Cards.Length;
                    var maxHandSize = 10; // TODO: Get this from HandState or configuration
                    var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                    
                    if (cardsToDraw > 0)
                    {
                        Console.WriteLine($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size");
                        handManager.Hand.DrawAndAppend(cardsToDraw);
                        Console.WriteLine($"[TurnStartCommand] Hand now has {handManager.Hand.Cards.Length} cards");
                    }
                    else
                    {
                        Console.WriteLine("[TurnStartCommand] Hand already at max size, no cards to draw");
                    }
                });
            }
            else
            {
                Console.WriteLine("[TurnStartCommand] WARNING: QueuedActionsManager not available, executing immediately");
                // Fallback to immediate execution
                var currentCardCount = handManager.Hand.Cards.Length;
                var maxHandSize = 10;
                var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                
                if (cardsToDraw > 0)
                {
                    Console.WriteLine($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size");
                    handManager.Hand.DrawAndAppend(cardsToDraw);
                }
            }

            // Transition to CardSelection phase (where player can select cards)
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
            var newState = currentState.WithPhase(newPhaseState);
            
            Console.WriteLine($"[TurnStartCommand] Turn started - new phase: {newState.Phase.CurrentPhase}");
            
            return newState;
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // Turn start typically can't be undone
            return null;
        }
    }
}