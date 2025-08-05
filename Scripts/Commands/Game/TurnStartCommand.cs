using System;
using System.Linq;
using Scripts.State;
using Scripts.Config;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to handle turn start - draws cards to max hand size and transitions to CardSelection
    /// </summary>
    public class TurnStartCommand : IGameCommand
    {
        private readonly ILogger _logger;
        private readonly GameCommandProcessor _commandProcessor;

        public TurnStartCommand()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<GameCommandProcessor>();
        }

        public string GetDescription() => "Start Turn";

        public bool CanExecute(IGameStateData currentState)
        {
            // Can only start turn from TurnStart phase
            return currentState.Phase.CurrentPhase == GamePhase.TurnStart;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            _logger?.LogInfo("[TurnStartCommand] Execute() called!");
            _logger?.LogInfo($"[TurnStartCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                _logger?.LogError("[TurnStartCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Queue the draw action to fill hand to maximum size
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                _logger?.LogInfo("[TurnStartCommand] Queuing card draw to max hand size...");
                
                queuedActionsManager.QueueAction(() =>
                {
                    var currentCardCount = _commandProcessor?.CurrentState?.Hand.Cards.Count ?? 0;
                    var maxHandSize = GameConfig.DEFAULT_MAX_HAND_SIZE;
                    var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                    
                    if (cardsToDraw > 0)
                    {
                        _logger?.LogInfo($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size");
                        handManager.Hand.DrawAndAppend(cardsToDraw);
                        _logger?.LogInfo($"[TurnStartCommand] Hand now has {_commandProcessor?.CurrentState?.Hand.Cards.Count} cards");
                    }
                    else
                    {
                        _logger?.LogInfo("[TurnStartCommand] Hand already at max size, no cards to draw");
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[TurnStartCommand] WARNING: QueuedActionsManager not available, executing immediately");
                // Fallback to immediate execution
                var currentCardCount = _commandProcessor?.CurrentState?.Hand.Cards.Count ?? 0;
                var maxHandSize = 10;
                var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                
                if (cardsToDraw > 0)
                {
                    _logger?.LogInfo($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size");
                    handManager.Hand.DrawAndAppend(cardsToDraw);
                }
            }

            // Transition to CardSelection phase (where player can select cards)
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
            var newState = currentState.WithPhase(newPhaseState);
            
            _logger?.LogInfo($"[TurnStartCommand] Turn started - new phase: {newState.Phase.CurrentPhase}");
            
            return newState;
        }
    }
}