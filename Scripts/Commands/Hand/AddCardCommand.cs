using Scripts.State;
using Godot;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;
using System;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to add a new card to the player's hand in GameState
    /// Used when cards are drawn/created after initial game setup
    /// </summary>
    public class AddCardCommand : GameCommand
    {
        private readonly SpellCardResource _spellCardResource;
        private readonly ContainerType _containerType;
        private readonly int _position;

        public AddCardCommand(SpellCardResource spellCardResource, ContainerType containerType, int position = -1) : base(true)
        {
            _spellCardResource = spellCardResource;
            _containerType = containerType;
            _position = position;
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;
            if (_spellCardResource == null) return false;

            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            if (_commandProcessor.CurrentState.Hand.Count >= _commandProcessor.CurrentState.Hand.MaxHandSize) return false;

            return true;
        }

        public override CommandResult ExecuteWithResult()
        {
            var currentState = _commandProcessor.CurrentState;
            GD.Print($"[AddCardCommand] Adding card {_spellCardResource.CardName} to GameState at position {_position}");

            var newCardState = new CardState(
                cardId: Guid.NewGuid().ToString(),
                resource: _spellCardResource,
                isSelected: false,
                isDragging: false,
                position: _position >= 0 ? _position : currentState.Hand.Count,
                containerType: _containerType
            );

            var newHandState = currentState.Hand.WithAddedCard(newCardState);
            var newState = currentState.WithHand(newHandState);

            GD.Print($"[AddCardCommand] Card {_spellCardResource.CardName} added to GameState at position {newCardState.Position+1} successfully. Hand now has {newHandState.Count} cards");

            (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(.1f).Timeout += _commandProcessor.NotifyBlockingCommandFinished;

            return CommandResult.Success(newState);
        }

        public override string GetDescription()
        {
            return $"Add card {_spellCardResource.CardName} to hand at position {_position}";
        }
    }
}