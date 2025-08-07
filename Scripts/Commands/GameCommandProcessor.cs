using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;
using Scripts.Commands.Game;

namespace Scripts.Commands
{
    public class GameCommandProcessor : IGameCommandProcessor
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private IGameStateData _currentState;
        private readonly Queue<GameCommand> _commandQueue = new();
        private bool _isProcessingQueue = false;

        public GameCommandProcessor()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _currentState = GameState.CreateInitial();
        }

        public IGameStateData CurrentState => _currentState;
        public event Action<IGameStateData, IGameStateData> StateChanged;

        public bool ExecuteCommand(GameCommand command)
        {
            if (command == null)
            {
                _logger.LogError("Cannot execute null command");
                return false;
            }

            if (!command.CanExecute())
            {
                _logger.LogWarning($"Command rejected: {command.GetDescription()}");
                return false;
            }

            try
            {
                var previousState = _currentState;
                var result = command.ExecuteWithResult();

                if (!result.IsSuccess)
                {
                    _logger.LogError($"Command failed: {result.ErrorMessage}");
                    return false;
                }

                if (result.NewState == null)
                {
                    _logger.LogError("Command returned null state");
                    return false;
                }

                if (!result.NewState.IsValid())
                {
                    _logger.LogError("Command resulted in invalid state");
                    return false;
                }

                _currentState = result.NewState;
                StateChanged?.Invoke(previousState, result.NewState);

                _eventBus?.Publish(new GameStateChangedEventData
                {
                    PreviousState = previousState,
                    NewState = result.NewState,
                    ExecutedCommand = command
                });

                foreach (var followUpCommand in result.FollowUpCommands)
                {
                    _commandQueue.Enqueue(followUpCommand);
                }

                ProcessQueuedCommands();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception executing command", ex);
                return false;
            }
        }

        private void ProcessQueuedCommands()
        {
            if (_isProcessingQueue) return;

            _isProcessingQueue = true;
            try
            {
                while (_commandQueue.Count > 0)
                {
                    var queuedCommand = _commandQueue.Dequeue();
                    ExecuteCommand(queuedCommand);
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        public void SetState(IGameStateData newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));

            if (!newState.IsValid())
            {
                throw new ArgumentException("Cannot set invalid game state", nameof(newState));
            }

            var previousState = _currentState;
            _currentState = newState;

            StateChanged?.Invoke(previousState, newState);

            _eventBus?.Publish(new GameStateChangedEventData
            {
                PreviousState = previousState,
                NewState = newState,
                ExecutedCommand = null
            });
        }
    }
}