using Scripts.State;

namespace Scripts.Commands
{

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

        public abstract bool CanExecute();

        public abstract void Execute(CommandCompletionToken token);

        public abstract string GetDescription();
    }
}