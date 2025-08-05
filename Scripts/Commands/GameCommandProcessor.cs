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
        private readonly CommandHistory _commandHistory;
        private IGameStateData _currentState;

        public GameCommandProcessor(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
            _commandHistory = new CommandHistory();
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

            // Sync GameState with real game before validation
            SyncGameStateWithRealGame();

            // Validate command can be executed
            if (!command.CanExecute(_currentState))
            {
                LogWarning($"Command rejected: {command.GetDescription()} - Cannot execute in current state - {_currentState.Phase.PhaseDescription}");
                return false;
            }

            try
            {
                // Store previous state for undo and events
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

                // Add to command history for undo/redo
                _commandHistory.AddCommand(command, previousState);

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
        /// Attempts to undo the last command
        /// </summary>
        /// <returns>True if undo was successful</returns>
        public bool UndoLastCommand()
        {
            if (!_commandHistory.CanUndo)
            {
                LogWarning("Cannot undo - no commands in history");
                return false;
            }

            try
            {
                var (lastCommand, previousState) = _commandHistory.GetLastCommand();
                var undoCommand = lastCommand.CreateUndoCommand(previousState);

                if (undoCommand == null)
                {
                    LogError($"Cannot create undo command for: {lastCommand.GetDescription()}");
                    return false;
                }

                // Execute the undo command (this will add it to history as well)
                var success = ExecuteCommand(undoCommand);

                if (success)
                {
                    // Remove the original command from history since it's been undone
                    _commandHistory.RemoveLastCommand();
                    LogInfo($"Successfully undid command: {lastCommand.GetDescription()}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Exception during undo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the game state directly (used for initialization or loading saved games)
        /// </summary>
        /// <param name="newState">The new game state</param>
        /// <param name="clearHistory">Whether to clear command history</param>
        public void SetState(IGameStateData newState, bool clearHistory = true)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));

            if (!newState.IsValid())
            {
                throw new ArgumentException("Cannot set invalid game state", nameof(newState));
            }

            var previousState = _currentState;
            _currentState = newState;

            if (clearHistory)
            {
                _commandHistory.Clear();
            }

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
        /// Gets the command history for debugging
        /// </summary>
        public IReadOnlyList<string> GetCommandHistory()
        {
            return _commandHistory.GetCommandDescriptions();
        }

        /// <summary>
        /// Clears all command history
        /// </summary>
        public void ClearHistory()
        {
            _commandHistory.Clear();
            LogInfo("Command history cleared");
        }

        /// <summary>
        /// Gets the current state summary for debugging
        /// </summary>
        public string GetStateSummary()
        {
            return _currentState.ToString();
        }

        /// <summary>
        /// Syncs the GameState with the real game objects to ensure single source of truth
        /// </summary>
        private void SyncGameStateWithRealGame()
        {
            try
            {
                // Get real game objects
                var handManager = ServiceLocator.GetService<IHandManager>();
                if (handManager?.Hand == null) return;

                var realHand = handManager.Hand;
                
                // Convert real cards to CardState objects
                var cardStates = realHand.Cards.Select(card => new CardState(
                    cardId: card.GetInstanceId().ToString(),
                    isSelected: card.IsSelected,
                    isDragging: card.IsDragging,
                    position: 0
                )).ToList();

                // Get selected card IDs
                var selectedCardIds = realHand.SelectedCards
                    .Select(card => card.GetInstanceId().ToString())
                    .ToList();

                // Create updated HandState with real data
                var newHandState = new HandState(
                    cards: cardStates,
                    selectedCardIds: selectedCardIds,
                    maxHandSize: 10,
                    isLocked: false
                );

                // Update the current state with real data
                _currentState = _currentState.WithHand(newHandState);

                LogInfo($"Synced GameState: {cardStates.Count} cards, {selectedCardIds.Count} selected");
            }
            catch (Exception ex)
            {
                LogError($"Failed to sync GameState with real game: {ex.Message}");
            }
        }

        private void LogInfo(string message)
        {
            // TODO: Use proper logging system
            GD.Print($"[GameCommandProcessor] INFO: {message}");
        }

        private void LogWarning(string message)
        {
            // TODO: Use proper logging system
            GD.Print($"[GameCommandProcessor] WARNING: {message}");
        }

        private void LogError(string message)
        {
            // TODO: Use proper logging system
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