using Scripts.State;

namespace Scripts.Commands
{
    /// <summary>
    /// Base class for all game commands that modify game state.
    /// Commands must be immutable and side-effect free.
    /// </summary>
    public abstract class GameCommand
    {
        protected readonly ILogger _logger;
        protected readonly IGameCommandProcessor _commandProcessor;
        public bool IsBlocking { get; init; } = false;

        public GameCommand(bool isBlocking = false)
        {
            IsBlocking = isBlocking;
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        /// <summary>
        /// Checks if the command can be executed with the current game state
        /// </summary>
        public abstract bool CanExecute();

        /// <summary>
        /// Executes the command and returns a CommandResult with new state and follow-up actions
        /// </summary>
        public abstract void Execute(CommandCompletionToken token);

        /// <summary>
        /// Gets a description of what this command does
        /// </summary>
        public abstract string GetDescription();
    }
}