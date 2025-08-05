using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands
{
    /// <summary>
    /// Central processor for all game commands.
    /// Validates, executes, and tracks command history.
    /// </summary>
    public class GameCommandProcessor
    {
        private readonly IEventBus _eventBus;
        private IGameStateData _currentState;

        public GameCommandProcessor(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
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
        public bool ExecuteCommand(IGameCommand command)
        {
            if (command == null)
            {
                LogError("Cannot execute null command");
                return false;
            }

            // REMOVED: SyncGameStateWithRealGame() - GameState is the single source of truth
            // The visual components should sync FROM GameState, not TO GameState

            // Validate command can be executed
            if (!command.CanExecute(_currentState))
            {
                LogWarning($"Command rejected: {command.GetDescription()} - Cannot execute in current state - {_currentState.Phase.PhaseDescription}");
                return false;
            }

            try
            {
                // Store previous state for events
                var previousState = _currentState;

                // Execute command to get new state
                var newState = command.Execute(_currentState);

                // Validate the new state
                if (newState == null)
                {
                    LogError($"Command {command.GetDescription()} returned null state");
                    return false;
                }

                if (!newState.IsValid())
                {
                    LogError($"Command {command.GetDescription()} resulted in invalid state");
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

                LogInfo($"Command executed successfully: {command.GetDescription()}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Exception executing command {command.GetDescription()}: {ex.Message}");
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

            LogInfo("Game state set directly");
        }

        /// <summary>
        /// Gets the current state summary for debugging
        /// </summary>
        public string GetStateSummary()
        {
            return _currentState.ToString();
        }

        private void LogInfo(string message)
        {
            GD.Print($"[GameCommandProcessor] INFO: {message}");
        }

        private void LogWarning(string message)
        {
            GD.Print($"[GameCommandProcessor] WARNING: {message}");
        }

        private void LogError(string message)
        {
            GD.Print($"[GameCommandProcessor] ERROR: {message}");
        }
    }

    /// <summary>
    /// Event data for game state changes
    /// </summary>
    public class GameStateChangedEventData
    {
        public IGameStateData PreviousState { get; set; }
        public IGameStateData NewState { get; set; }
        public IGameCommand ExecutedCommand { get; set; }
    }
}