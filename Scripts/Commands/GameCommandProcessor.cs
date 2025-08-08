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
        private IGameStateData _currentState;
        private readonly Queue<GameCommand> _commandQueue = new();
        private bool _isProcessingQueue = false;

        public GameCommandProcessor()
        {
            _logger = ServiceLocator.GetService<ILogger>();
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

            if (_isProcessingQueue && command.IsQueued)
            {
                _commandQueue.Append(command);
                return true;
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

                    foreach (var followUpCommand in result.FollowUpCommands)
                    {
                        _commandQueue.Enqueue(followUpCommand);
                    }

                    if (command.IsBlocking)
                    {
                        _isProcessingQueue = true;
                    }
                    else
                    {
                        ProcessQueuedCommands();
                    }


                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception executing command", ex);
                    return false;
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
        }

        public void NotifyBlockingCommandFinished()
        {
            _isProcessingQueue = false;
            ProcessQueuedCommands();
        }

        
        private void ProcessQueuedCommands()
        {
            if (_isProcessingQueue) return;

            _isProcessingQueue = true;
            try
            {
                GameCommand queuedCommand = null;
                while (_commandQueue.Count > 0 && queuedCommand?.IsBlocking != true)
                {
                    queuedCommand = _commandQueue.Dequeue();
                    ExecuteCommand(queuedCommand);
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }
    }
}