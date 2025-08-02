using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using System.Linq;

namespace Maximagus.Scripts.Input
{
    public partial class GameInputManager : Node
    {
        private IHandManager _handManager;
        private ISpellProcessingManager _spellProcessor;

        public override void _Ready()
        {
            _handManager = ServiceLocator.GetService<IHandManager>();
            _spellProcessor = ServiceLocator.GetService<ISpellProcessingManager>();

            if (_spellProcessor == null)
            {
                GD.PrintErr("GameInputHandler: SpellProcessor not found in ServiceLocator.");
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Enter:
                        HandlePlayAction();
                        break;
                    case Key.Delete:
                        HandleDiscardAction();
                        break;
                }
            }
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