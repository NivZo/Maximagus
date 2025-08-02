using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using System.Linq;

namespace Maximagus.Scripts.Input
{
    public partial class GameInputManager : Node
    {
        private IGameStateManager _gameStateManager;
        private IHandManager _handManager;
        private ISpellProcessingManager _spellProcessor;

        public override void _Ready()
        {
            GD.Print("Game input manager init");
            _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            _handManager = ServiceLocator.GetService<IHandManager>();
            _spellProcessor = ServiceLocator.GetService<ISpellProcessingManager>();
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
            var selectedCards = new Array<SpellCardResource>(Hand.Instance.SelectedCards.Select(c => c.Resource));
            if (_handManager.SubmitHand(selectedCards, HandActionType.Play))
            {
                _spellProcessor.ProcessSpell(selectedCards);
            }
        }

        private void HandleDiscardAction()
        {
            var selectedCards = new Array<SpellCardResource>(Hand.Instance.SelectedCards.Select(c => c.Resource));
            _handManager.SubmitHand(selectedCards, HandActionType.Discard);
        }
    }
}