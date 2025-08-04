using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using System.Linq;
using Maximagus.Scripts.Events;

namespace Maximagus.Scripts.Input
{
    public partial class GameInputManager : Node
    {
        private IGameStateManager _gameStateManager;
        private IHandManager _handManager;
        private ISpellProcessingManager _spellProcessor;
        private IEventBus _eventBus;

        public override void _Ready()
        {
            GD.Print("Game input manager init");
            _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            _handManager = ServiceLocator.GetService<IHandManager>();
            _spellProcessor = ServiceLocator.GetService<ISpellProcessingManager>();
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
            _gameStateManager.TriggerEvent(GameStateEvent.StartGame);
        }

        private void HandlePlayAction()
        {
            var selectedCards = new Array<Card>(Hand.Instance.SelectedCards);
            var currentHandCards = new Array<Card>(Hand.Instance.Cards);
            _eventBus.Publish(new PlayCardsRequestedEvent { SelectedCards = selectedCards, CurrentHandCards = currentHandCards });
        }

        private void HandleDiscardAction()
        {
            var selectedCards = new Array<Card>(Hand.Instance.SelectedCards);
            var currentHandCards = new Array<Card>(Hand.Instance.Cards);
            _eventBus.Publish(new DiscardCardsRequestedEvent { SelectedCards = selectedCards, CurrentHandCards = currentHandCards });
        }
    }
}