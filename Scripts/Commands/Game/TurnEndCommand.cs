using System;
using System.Linq;
using Scripts.State;
using Scripts.Config;
using Scripts.Commands;
using Maximagus.Scripts.Managers;
using Scripts.Commands.Hand;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;

namespace Scripts.Commands.Game
{

    public class TurnEndCommand : GameCommand
    {

        public TurnEndCommand() : base()
        {
        }

        public override string GetDescription() => "Start Turn";

        public override bool CanExecute()
        {
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.TurnEnd;
        }


        public override void Execute(CommandCompletionToken token)
        {
            // First transition to TurnStart phase (intermediate phase for processing)
            var currentState = _commandProcessor.CurrentState;
            // Move all played cards to Discarded in centralized CardsState
            var playedCards = currentState.Cards.PlayedCards.Select(c => c.CardId).ToArray();
            var newCardsState = currentState.Cards.WithMovedToContainer(playedCards, ContainerType.DiscardedCards);

            // Create status effect commands for end of turn
            var triggerEndOfTurnCommand = new TriggerStatusEffectsCommand(StatusEffectTrigger.EndOfTurn);
            var processEndOfTurnDecayCommand = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.EndOfTurn);
            var processReduceByOneDecayCommand = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.ReduceByOneEndOfTurn);

            // Transition to TurnStart phase (natural end state)
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.TurnStart);
            var finalState = _commandProcessor.CurrentState
                .WithCards(newCardsState)
                .WithPhase(newPhaseState);

            _logger?.LogInfo($"[TurnEndCommand] Turn ended - cards played: {playedCards.Count()}, final phase: {finalState.Phase.CurrentPhase}");

            // Execute status effect processing before transitioning to next turn
            var followUpCommands = new GameCommand[] 
            {
                triggerEndOfTurnCommand,
                processEndOfTurnDecayCommand,
                processReduceByOneDecayCommand,
                new TurnStartCommand()
            };

            token.Complete(CommandResult.Success(finalState, followUpCommands));
        }
    }
}