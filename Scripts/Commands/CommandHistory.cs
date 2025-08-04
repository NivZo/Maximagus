using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands
{
    /// <summary>
    /// Tracks command history for undo/redo functionality
    /// </summary>
    public class CommandHistory
    {
        private readonly List<CommandHistoryEntry> _history;
        private const int MaxHistorySize = 100;

        public CommandHistory()
        {
            _history = new List<CommandHistoryEntry>();
        }

        /// <summary>
        /// Gets whether there are commands that can be undone
        /// </summary>
        public bool CanUndo => _history.Count > 0;

        /// <summary>
        /// Gets the total number of commands in history
        /// </summary>
        public int Count => _history.Count;

        /// <summary>
        /// Adds a command to the history
        /// </summary>
        /// <param name="command">The executed command</param>
        /// <param name="previousState">The state before the command was executed</param>
        public void AddCommand(IGameCommand command, IGameStateData previousState)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (previousState == null) throw new ArgumentNullException(nameof(previousState));

            var entry = new CommandHistoryEntry
            {
                Command = command,
                PreviousState = previousState,
                ExecutedAt = DateTime.UtcNow
            };

            _history.Add(entry);

            // Limit history size to prevent memory issues
            if (_history.Count > MaxHistorySize)
            {
                _history.RemoveAt(0);
            }
        }

        /// <summary>
        /// Gets the last command and its previous state
        /// </summary>
        /// <returns>Tuple of (command, previousState)</returns>
        public (IGameCommand Command, IGameStateData PreviousState) GetLastCommand()
        {
            if (!CanUndo)
                throw new InvalidOperationException("No commands in history to undo");

            var lastEntry = _history[_history.Count - 1];
            return (lastEntry.Command, lastEntry.PreviousState);
        }

        /// <summary>
        /// Removes the last command from history (used after successful undo)
        /// </summary>
        public void RemoveLastCommand()
        {
            if (!CanUndo)
                throw new InvalidOperationException("No commands in history to remove");

            _history.RemoveAt(_history.Count - 1);
        }

        /// <summary>
        /// Gets a list of command descriptions for debugging
        /// </summary>
        /// <returns>Read-only list of command descriptions</returns>
        public IReadOnlyList<string> GetCommandDescriptions()
        {
            return _history.Select(entry => 
                $"{entry.ExecutedAt:HH:mm:ss.fff} - {entry.Command.GetDescription()}")
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets a specific command entry by index (0 = oldest)
        /// </summary>
        /// <param name="index">Index of the command entry</param>
        /// <returns>Command history entry</returns>
        public CommandHistoryEntry GetEntry(int index)
        {
            if (index < 0 || index >= _history.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _history[index];
        }

        /// <summary>
        /// Gets all command entries
        /// </summary>
        /// <returns>Read-only list of all command history entries</returns>
        public IReadOnlyList<CommandHistoryEntry> GetAllEntries()
        {
            return _history.AsReadOnly();
        }

        /// <summary>
        /// Clears all command history
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }

        /// <summary>
        /// Gets the most recent commands up to a specified count
        /// </summary>
        /// <param name="count">Maximum number of recent commands to return</param>
        /// <returns>List of recent command descriptions</returns>
        public IReadOnlyList<string> GetRecentCommands(int count = 10)
        {
            var recentCount = Math.Min(count, _history.Count);
            return _history
                .Skip(_history.Count - recentCount)
                .Select(entry => entry.Command.GetDescription())
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Searches for commands containing specific text in their description
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="ignoreCase">Whether to ignore case in search</param>
        /// <returns>List of matching command descriptions with timestamps</returns>
        public IReadOnlyList<string> SearchCommands(string searchText, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(searchText))
                return new List<string>().AsReadOnly();

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return _history
                .Where(entry => entry.Command.GetDescription().Contains(searchText, comparison))
                .Select(entry => $"{entry.ExecutedAt:HH:mm:ss.fff} - {entry.Command.GetDescription()}")
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Represents a single entry in the command history
    /// </summary>
    public class CommandHistoryEntry
    {
        /// <summary>
        /// The command that was executed
        /// </summary>
        public IGameCommand Command { get; set; }

        /// <summary>
        /// The game state before the command was executed
        /// </summary>
        public IGameStateData PreviousState { get; set; }

        /// <summary>
        /// When the command was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Optional additional metadata about the command execution
        /// </summary>
        public string Metadata { get; set; }
    }
}