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
        private readonly IGameCommandProcessor _commandProcessor;

        public TurnStartCommand()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public string GetDescription() => "Start Turn";

        public bool CanExecute(IGameStateData currentState)
        {
            // Can only start turn from TurnStart phase
            return currentState.Phase.CurrentPhase == GamePhase.Menu || currentState.Phase.CurrentPhase == GamePhase.TurnEnd;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            _logger?.LogInfo("[TurnStartCommand] Execute() called!");
            _commandProcessor.SetState(currentState.WithPhase(currentState.Phase.WithPhase(GamePhase.TurnStart)));

            _logger?.LogInfo($"[TurnStartCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                _logger?.LogError("[TurnStartCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Queue the draw action to fill hand to maximum size using state-driven approach
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            if (queuedActionsManager != null)
            {
                _logger?.LogInfo("[TurnStartCommand] Queuing card draw to max hand size (state-driven)...");
                
                queuedActionsManager.QueueAction(() =>
                {
                    var currentCardCount = _commandProcessor?.CurrentState?.Hand.Cards.Count ?? 0;
                    var maxHandSize = GameConfig.DEFAULT_MAX_HAND_SIZE;
                    var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                    
                    if (cardsToDraw > 0)
                    {
                        _logger?.LogInfo($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size (state-driven)");
                        
                        // State-driven approach: Draw cards one by one and update state with each
                        for (int i = 0; i < cardsToDraw; i++)
                        {
                            // Get card resource from deck
                            var cardResourceId = handManager.DrawCard();
                            if (string.IsNullOrEmpty(cardResourceId))
                            {
                                _logger?.LogError("[TurnStartCommand] Failed to get card from deck!");
                                continue;
                            }
                            
                            // Create AddCardCommand to update state
                            var addCardCommand = new Commands.Hand.AddCardCommand(cardResourceId);
                            
                            // Execute command - this updates state, which will trigger UI updates
                            var success = _commandProcessor.ExecuteCommand(addCardCommand);
                            
                            if (!success)
                            {
                                _logger?.LogError($"[TurnStartCommand] Failed to add card {cardResourceId} to hand state!");
                                break; // Stop if we can't add more cards
                            }
                            
                            _logger?.LogInfo($"[TurnStartCommand] Added card {cardResourceId} to hand state ({i+1}/{cardsToDraw})");
                        }
                        
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
                // Fallback to immediate execution with state-driven approach
                var currentCardCount = _commandProcessor?.CurrentState?.Hand.Cards.Count ?? 0;
                var maxHandSize = GameConfig.DEFAULT_MAX_HAND_SIZE;
                var cardsToDraw = Math.Max(0, maxHandSize - currentCardCount);
                
                if (cardsToDraw > 0)
                {
                    _logger?.LogInfo($"[TurnStartCommand] Drawing {cardsToDraw} cards to reach max hand size (state-driven)");
                    
                    for (int i = 0; i < cardsToDraw; i++)
                    {
                        // Get card resource from deck
                        var cardResourceId = handManager.DrawCard();
                        if (string.IsNullOrEmpty(cardResourceId))
                        {
                            _logger?.LogError("[TurnStartCommand] Failed to get card from deck!");
                            continue;
                        }
                        
                        // Create AddCardCommand to update state
                        var addCardCommand = new Commands.Hand.AddCardCommand(cardResourceId);
                        
                        // Execute command - this updates state, which will trigger UI updates
                        var success = _commandProcessor.ExecuteCommand(addCardCommand);
                        
                        if (!success)
                        {
                            _logger?.LogError($"[TurnStartCommand] Failed to add card {cardResourceId} to hand state!");
                            break; // Stop if we can't add more cards
                        }
                    }
                }
            }

            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
            var newState = currentState.WithPhase(newPhaseState);
            
            _logger?.LogInfo($"[TurnStartCommand] Turn started - new phase: {newState.Phase.CurrentPhase}");

            return newState;
        }
    }
}