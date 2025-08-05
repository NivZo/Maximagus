using Godot;
using Maximagus.Scripts.Events;

namespace Maximagus.Scripts.Input
{
    public partial class GameInputManager : Node
    {
        private IEventBus _eventBus;

        public override void _Ready()
        {
            GD.Print("Game input manager init");
            _eventBus = ServiceLocator.GetService<IEventBus>();
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
            // Use event bus instead of game state manager
            _eventBus.Publish(new StartGameRequestedEvent());
        }

        private void HandlePlayAction()
        {
            _eventBus.Publish(new PlayCardsRequestedEvent());
        }

        private void HandleDiscardAction()
        {
            _eventBus.Publish(new DiscardCardsRequestedEvent());
        }
    }
}