using System;
using System.Linq;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Spell;
using Godot;
using Maximagus.Scripts.Enums;
using System.Collections.Generic;
using Maximagus.Resources.Definitions.Actions;

namespace Scripts.Commands.Game
{

    public class SpellCastCommand : GameCommand
    {
        public SpellCastCommand() : base(true)
        {
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState?.Phase?.CurrentPhase != GamePhase.SpellCasting)
                return false;

            // Must have played cards to cast a spell
            var playedCards = currentState.Cards.PlayedCards?.ToArray();
            if (playedCards == null || playedCards.Length == 0)
            {
                _logger.LogWarning("[SpellCastCommand] Cannot cast spell - no played cards found");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            // Get played cards ordered by position
            var playedCards = currentState.Cards.PlayedCards
                .OrderBy(c => c.Position)
                .ToArray();

            _logger.LogInfo($"[SpellCastCommand] Starting snapshot-based spell processing with {playedCards.Length} cards");

            // Create command chain for spell processing
            var commandChain = new List<GameCommand>
            {
                // 1. Start the spell
                new StartSpellCommand()
            };

            // 2. Pre-calculate all actions for the entire spell using EncounterState snapshots
            // This generates and stores complete snapshots for all actions
            var allActions = playedCards.SelectMany(c => c.Resource.Actions).ToList();
            commandChain.Add(new PreCalculateSpellCommand(allActions));

            // 3. Execute each card action sequentially using pre-calculated snapshots
            // Each ExecuteCardActionCommand will fetch and apply its corresponding snapshot
            foreach (var cardState in playedCards)
            {
                foreach (var action in cardState.Resource.Actions)
                {
                    commandChain.Add(new ExecuteCardActionCommand(action, cardState.CardId));
                }
            }

            // 4. Complete the spell and clean up snapshots
            var castCardResources = playedCards.Select(c => c.Resource).ToList();
            commandChain.Add(new CompleteSpellCommand(castCardResources, true));

            // 5. Transition to TurnEnd phase
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnEnd);
            var newState = currentState.WithPhase(newPhaseState);
            commandChain.Add(new TurnEndCommand());

            _logger.LogInfo($"[SpellCastCommand] Created snapshot-based command chain with {commandChain.Count} commands");

            token.Complete(CommandResult.Success(newState, commandChain));
        }

        public override string GetDescription()
        {
            return "Process spell casting with command chain";
        }
    }
}