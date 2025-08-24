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

        private Deck _deck;
        
        public ImmutableArray<SpellCardResource> Cards => _commandProcessor.CurrentState.Cards.HandCards.Select(card => card.Resource).ToImmutableArray();
        public ImmutableArray<SpellCardResource> SelectedCards => _commandProcessor.CurrentState.Cards.SelectedInHand.Select(card => card.Resource).ToImmutableArray();
        public SpellCardResource DraggingCard => _commandProcessor.CurrentState.Cards.DraggingInHand?.Resource;

        private IGameStateData _currentState => _commandProcessor.CurrentState;

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();

            _deck = new Deck(20);
        }

        public int GetCardsToDraw()
        {
            return Math.Max(0, _currentState.Hand.MaxHandSize - _currentState.Cards.InHandCount);
        }

        public AddCardCommand GetDrawCardCommand()
        {
            var resource = _deck.GetNext();
            var command = new AddCardCommand(resource, ContainerType.Hand);
            return command;
        }
    }
}