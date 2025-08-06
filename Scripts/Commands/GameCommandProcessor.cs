using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;
using Scripts.Commands.Game;

namespace Scripts.Commands
{
    /// <summary>
    /// Central processor for all game commands.
    /// Validates, executes, and tracks command history.
    /// </summary>
    public class GameCommandProcessor : IGameCommandProcessor
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private IGameStateData _currentState;

        public GameCommandProcessor()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _currentState = GameState.CreateInitial();
        }

        /// <summary>
        /// Gets the current game state (read-only)
        /// </summary>
        public IGameStateData CurrentState => _currentState;

        /// <summary>
        /// Event fired when game state changes
        /// </summary>
        public event Action<IGameStateData, IGameStateData> StateChanged;

        /// <summary>
        /// Executes a command if it's valid
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>True if command was executed successfully</returns>
        public bool ExecuteCommand(GameCommand command)
        {
            if (command == null)
            {
                _logger.LogError("Cannot execute null command");
                return false;
            }

            // Validate command can be executed
            if (!command.CanExecute())
            {
                _logger.LogWarning($"Command rejected: {command.GetDescription()} - Cannot execute in current state - {_currentState.Phase.PhaseDescription}");
                return false;
            }

            try
            {
                // Store previous state for events
                var previousState = _currentState;

                // Execute command to get new state
                _logger.LogInfo($"Executing command: {command.GetType()}");
                var newState = command.Execute();

                // Validate the new state
                if (newState == null)
                {
                    _logger.LogError($"Command {command.GetDescription()} returned null state");
                    return false;
                }

                if (!newState.IsValid())
                {
                    _logger.LogError($"Command {command.GetDescription()} resulted in invalid state");
                    return false;
                }

                // Update current state
                _currentState = newState;

                // Fire state change event
                StateChanged?.Invoke(previousState, newState);

                // Publish state change event through event bus
                _eventBus?.Publish(new GameStateChangedEventData
                {
                    PreviousState = previousState,
                    NewState = newState,
                    ExecutedCommand = command
                });

                _logger.LogInfo($"Command executed successfully: {command.GetDescription()}");

                if (_currentState.Phase != previousState.Phase)
                {
                    _logger.LogInfo($"Phase changed from {previousState.Phase.CurrentPhase} to {_currentState.Phase.CurrentPhase} - executing phase command");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception executing command {command.GetDescription()}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the game state directly (used for initialization or loading saved games)
        /// </summary>
        /// <param name="newState">The new game state</param>
        public void SetState(IGameStateData newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));

            if (!newState.IsValid())
            {
                throw new ArgumentException("Cannot set invalid game state", nameof(newState));
            }

            var previousState = _currentState;
            _currentState = newState;

            // Fire state change event
            StateChanged?.Invoke(previousState, newState);

            // Publish state change event
            _eventBus?.Publish(new GameStateChangedEventData
            {
                PreviousState = previousState,
                NewState = newState,
                ExecutedCommand = null
            });

            _logger.LogInfo("Game state set directly");
        }
    }
}