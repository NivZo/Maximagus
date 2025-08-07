using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands
{
    /// <summary>
    /// Result of executing a command, containing new state and any follow-up actions
    /// </summary>
    public class CommandResult
    {
        public IGameStateData NewState { get; }
        public IEnumerable<GameCommand> FollowUpCommands { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private CommandResult(IGameStateData newState,
                             IEnumerable<GameCommand> followUpCommands = null,
                             bool success = true,
                             string errorMessage = null)
        {
            NewState = newState;
            FollowUpCommands = followUpCommands ?? Enumerable.Empty<GameCommand>();
            IsSuccess = success;
            ErrorMessage = errorMessage;
        }

        public static CommandResult Success(IGameStateData newState,
                                          IEnumerable<GameCommand> followUpCommands = null)
            => new(newState, followUpCommands);

        public static CommandResult Failure(string errorMessage)
            => new(null, success: false, errorMessage: errorMessage);
    }
}