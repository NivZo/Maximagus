using Godot;
using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Spells.Abstractions;
using Scripts.Commands.Hand;

namespace Maximagus.Scripts.Managers
{
    public class HandManager : IHandManager
    {        
        private ILogger _logger;
        private IGameCommandProcessor _commandProcessor;
        
        public ImmutableArray<SpellCardResource> Cards => _commandProcessor.CurrentState.Hand.Cards.Select(card => card.Resource).ToImmutableArray();
        public ImmutableArray<SpellCardResource> SelectedCards => _commandProcessor.CurrentState.Hand.Cards.Where(card => card.IsSelected).Select(card => card.Resource).ToImmutableArray();
        public SpellCardResource DraggingCard => _commandProcessor.CurrentState.Hand.DraggingCard.Resource;

        private IGameStateData _currentState => _commandProcessor.CurrentState;

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public int GetCardsToDraw()
        {
            return Math.Max(0, _currentState.Hand.MaxHandSize - _currentState.Hand.Cards.Where(card => card.ContainerType == ContainerType.Hand).Count());
        }

        public AddCardCommand GetDrawCardCommand()
        {
            var deck = new Deck();
            var resource = deck.GetNext();
            var command = new AddCardCommand(resource, ContainerType.Hand);
            return command;
        }
    }
}