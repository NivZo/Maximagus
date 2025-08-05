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
    public interface IGameCommandProcessor
    {
        public IGameStateData CurrentState { get; }

        /// <summary>
        /// Event fired when game state changes
        /// </summary>
        public event Action<IGameStateData, IGameStateData> StateChanged;

        /// <summary>
        /// Executes a command if it's valid
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>True if command was executed successfully</returns>
        public bool ExecuteCommand(IGameCommand command);

        public void SetState(IGameStateData newState);

        public string GetStateSummary();
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