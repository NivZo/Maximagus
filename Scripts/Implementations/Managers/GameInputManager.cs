using Godot;
using Scripts.Commands;
using Scripts.Commands.Game;
using Scripts.Commands.Hand;

namespace Maximagus.Scripts.Input
{
    /// <summary>
    /// PURE COMMAND SYSTEM: GameInputManager now executes commands directly
    /// No more legacy event publishing - replaced with direct command execution
    /// </summary>
    public partial class GameInputManager : Node
    {
        private GameCommandProcessor _commandProcessor;
        private ILogger _logger;

        public override void _Ready()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<GameCommandProcessor>();
            
            _logger?.LogInfo("GameInputManager initialized with pure command system");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Enter:
                        HandleStartGameAction();
                        break;
                    case Key.Space:
                        HandlePlayAction();
                        break;
                    case Key.Delete:
                        HandleDiscardAction();
                        break;
                }
            }
        }

        private void HandleStartGameAction()
        {
            // PURE COMMAND SYSTEM: Execute StartGameCommand directly
            if (_commandProcessor != null)
            {
                var command = new StartGameCommand();
                var success = _commandProcessor.ExecuteCommand(command);
                _logger?.LogInfo($"StartGameCommand executed: {success}");
            }
            else
            {
                _logger?.LogWarning("GameCommandProcessor not available for StartGameCommand");
            }
        }

        private void HandlePlayAction()
        {
            // PURE COMMAND SYSTEM: Execute PlayHandCommand directly
            if (_commandProcessor != null)
            {
                var command = new PlayHandCommand();
                var success = _commandProcessor.ExecuteCommand(command);
                _logger?.LogInfo($"PlayHandCommand executed: {success}");
            }
            else
            {
                _logger?.LogWarning("GameCommandProcessor not available for PlayHandCommand");
            }
        }

        private void HandleDiscardAction()
        {
            // PURE COMMAND SYSTEM: Would execute DiscardCardsCommand if implemented
            // For now, discarding is handled through HandManager legacy system
            _logger?.LogInfo("Discard action - handled by HandManager legacy system");
            
            // TODO: Implement DiscardCardsCommand when needed
            // var command = new DiscardCardsCommand();
            // var success = _commandProcessor.ExecuteCommand(command);
        }
    }
}