using Scripts.State;
using Godot;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;
using System;
using System.Linq;

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

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;

            // Determine default position at end of target container when not provided
            int containerCount = _containerType switch
            {
                ContainerType.Hand => currentState.Cards.InHandCount,
                ContainerType.PlayedCards => currentState.Cards.PlayedCards.Count(),
                ContainerType.DiscardedCards => currentState.Cards.DiscardedCards.Count(),
                _ => 0
            };
            var position = _position >= 0 ? _position : containerCount;

            _logger.LogInfo($"[AddCardCommand] Adding card {_spellCardResource.CardName} to {_containerType} at position {position}");

            var newCardState = new CardState(
                cardId: Guid.NewGuid().ToString(),
                resource: _spellCardResource,
                isSelected: false,
                isDragging: false,
                position: position,
                containerType: _containerType
            );

            var newCards = currentState.Cards.WithAddedCard(newCardState);
            var newState = currentState.WithCards(newCards);

            _logger.LogInfo($"[AddCardCommand] Card {_spellCardResource.CardName} added to {_containerType} at position {position}");

            (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(.1f).Timeout += () => token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return $"Add card {_spellCardResource.CardName} to hand at position {_position}";
        }
    }
}