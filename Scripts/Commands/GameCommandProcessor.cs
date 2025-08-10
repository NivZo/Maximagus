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
        private object _queueLock = new();

        public GameCommandProcessor()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _currentState = GameState.CreateInitial();
        }

        public IGameStateData CurrentState => _currentState;
        public event Action<IGameStateData, IGameStateData> StateChanged;

        public bool ExecuteCommand(GameCommand command)
        {
            if (!command.CanExecute())
            {
                _logger.LogWarning($"Command rejected: {command.GetDescription()}");
                return false;
            }

            lock (_queueLock)
            {
                if (_isProcessingQueue)
                {
                    _commandQueue.Enqueue(command);
                    return true;
                }

                try
                {
                    _isProcessingQueue = true;
                    var token = new CommandCompletionToken();
                    token.Subscribe(OnTokenCompletion);
                    command.Execute(token);
                    return true;
                }
                catch (Exception ex)
                {
                    _isProcessingQueue = false;
                    _logger.LogError("Exception executing command", ex);
                    return false;
                }
            }
        }

        private void OnTokenCompletion(CommandResult result)
        {
            lock (_queueLock)
            {
                try
                {
                    var previousState = _currentState;

                    if (!result.IsSuccess)
                    {
                        _logger.LogError($"Command failed: {result.ErrorMessage}");
                        return;
                    }

                    if (result.NewState == null)
                    {
                        _logger.LogError("Command returned null state");
                        return;
                    }

                    if (!result.NewState.IsValid())
                    {
                        _logger.LogError("Command resulted in invalid state");
                        return;
                    }

                    _currentState = result.NewState;
                    StateChanged?.Invoke(previousState, result.NewState);

                    foreach (var followUpCommand in result.FollowUpCommands)
                    {
                        _commandQueue.Enqueue(followUpCommand);
                    }
                }
                finally
                {
                    _isProcessingQueue = false;
                    ProcessNextQueuedCommand();
                }
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
            ProcessNextQueuedCommand();
        }

        
        private void ProcessNextQueuedCommand()
        {
            if (_isProcessingQueue) return;
            if (_commandQueue.Count == 0) return;

            var queuedCommand = _commandQueue.Dequeue();
            ExecuteCommand(queuedCommand);
        }
    }
}