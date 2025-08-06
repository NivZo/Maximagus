using Scripts.State;

namespace Scripts.Commands
{
    /// <summary>
    /// Base interface for all game commands that modify game state.
    /// Commands must be immutable and side-effect free.
    /// </summary>
    public abstract class GameCommand
    {
        protected readonly ILogger _logger;
        protected readonly IGameCommandProcessor _commandProcessor;

        public GameCommand()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public abstract bool CanExecute();

        public abstract IGameStateData Execute();

        public abstract string GetDescription();
    }
}