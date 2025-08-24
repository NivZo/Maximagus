using System;
using System.Linq;
using System.Collections.Generic;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Game;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Resources.Definitions.Actions;
using Godot;

namespace Tests.Commands
{
    /// <summary>
    /// Integration tests for the complete spell processing command chain.
    /// Tests the flow: SpellCastCommand -> StartSpellCommand -> ExecuteCardActionCommand(s) -> CompleteSpellCommand
    /// </summary>
    public class SpellProcessingIntegrationTests
    {
        private IGameCommandProcessor _commandProcessor;
        private ILogger _logger;

        public void Setup()
        {
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _logger = ServiceLocator.GetService<ILogger>();
        }

        public void TestCompleteSpellProcessingFlow()
        {
            Setup();
            
            // Arrange: Create initial state with played cards
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 10
            };

            var modifierAction = new ModifierActionResource
            {
                ModifierType = ModifierType.Add,
                Element = DamageType.Fire,
                Value = 5,
                IsConsumedOnUse = true
            };

            var spellCard1 = CreateTestSpellCard("card1", "Test Card 1", new ActionResource[] { damageAction });
            var spellCard2 = CreateTestSpellCard("card2", "Test Card 2", new ActionResource[] { modifierAction, damageAction });

            var cardState1 = new CardState("card1", spellCard1, position: 0, containerType: ContainerType.PlayedCards);
            var cardState2 = new CardState("card2", spellCard2, position: 1, containerType: ContainerType.PlayedCards);

            var cardsState = new CardsState(new[] { cardState1, cardState2 });
            var phaseState = new GamePhaseState().WithPhase(GamePhase.SpellCasting);
            var initialState = GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                phaseState,
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );

            _commandProcessor.SetState(initialState);

            // Act: Execute SpellCastCommand
            var spellCastCommand = new SpellCastCommand();
            var canExecute = spellCastCommand.CanExecute();
            
            Assert(canExecute, "SpellCastCommand should be able to execute with played cards in SpellCasting phase");

            var token = new CommandCompletionToken();
            var completed = false;
            CommandResult result = null;

            token.Subscribe((r) => {
                completed = true;
                result = r;
            });

            spellCastCommand.Execute(token);

            // Assert: Verify command chain was created
            Assert(completed, "SpellCastCommand should complete synchronously");
            Assert(result.IsSuccess, $"SpellCastCommand should succeed, but got: {result.ErrorMessage}");
            Assert(result.FollowUpCommands != null && result.FollowUpCommands.Any(), "SpellCastCommand should create follow-up commands");

            // Verify the command chain structure
            var commands = result.FollowUpCommands.ToArray();
            Assert(commands.Length >= 5, $"Expected at least 5 commands (Start + 3 actions + Complete + TurnEnd), got {commands.Length}");
            
            Assert(commands[0] is StartSpellCommand, "First command should be StartSpellCommand");
            Assert(commands[1] is ExecuteCardActionCommand, "Second command should be ExecuteCardActionCommand for first card's damage action");
            Assert(commands[2] is ExecuteCardActionCommand, "Third command should be ExecuteCardActionCommand for second card's modifier action");
            Assert(commands[3] is ExecuteCardActionCommand, "Fourth command should be ExecuteCardActionCommand for second card's damage action");
            Assert(commands[4] is CompleteSpellCommand, "Fifth command should be CompleteSpellCommand");
            Assert(commands[5] is TurnEndCommand, "Sixth command should be TurnEndCommand");

            // Verify phase transition
            Assert(result.NewState.Phase.CurrentPhase == GamePhase.TurnEnd, "Phase should transition to TurnEnd");

            GD.Print("[SpellProcessingIntegrationTests] TestCompleteSpellProcessingFlow passed");
        }

        public void TestSpellProcessingWithEmptyPlayedCards()
        {
            Setup();
            
            // Arrange: Create state with no played cards
            var cardsState = new CardsState(); // Empty cards
            var phaseState = new GamePhaseState().WithPhase(GamePhase.SpellCasting);
            var initialState = GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                phaseState,
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );

            _commandProcessor.SetState(initialState);

            // Act & Assert: SpellCastCommand should not be able to execute
            var spellCastCommand = new SpellCastCommand();
            var canExecute = spellCastCommand.CanExecute();
            
            Assert(!canExecute, "SpellCastCommand should not be able to execute with no played cards");

            GD.Print("[SpellProcessingIntegrationTests] TestSpellProcessingWithEmptyPlayedCards passed");
        }

        public void TestSpellProcessingInWrongPhase()
        {
            Setup();
            
            // Arrange: Create state in wrong phase
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 10
            };

            var spellCard = CreateTestSpellCard("card1", "Test Card", new ActionResource[] { damageAction });
            var cardState = new CardState("card1", spellCard, position: 0, containerType: ContainerType.PlayedCards);
            var cardsState = new CardsState(new[] { cardState });
            var phaseState = new GamePhaseState().WithPhase(GamePhase.CardSelection); // Wrong phase
            var initialState = GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                phaseState,
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );

            _commandProcessor.SetState(initialState);

            // Act & Assert: SpellCastCommand should not be able to execute
            var spellCastCommand = new SpellCastCommand();
            var canExecute = spellCastCommand.CanExecute();
            
            Assert(!canExecute, "SpellCastCommand should not be able to execute in CardSelection phase");

            GD.Print("[SpellProcessingIntegrationTests] TestSpellProcessingInWrongPhase passed");
        }

        public void TestCommandChainExecution()
        {
            Setup();
            
            // Arrange: Create state with a simple spell card
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 15
            };

            var spellCard = CreateTestSpellCard("card1", "Fire Bolt", new ActionResource[] { damageAction });
            var cardState = new CardState("card1", spellCard, position: 0, containerType: ContainerType.PlayedCards);
            var cardsState = new CardsState(new[] { cardState });
            var phaseState = new GamePhaseState().WithPhase(GamePhase.SpellCasting);
            var initialState = GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                phaseState,
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );

            _commandProcessor.SetState(initialState);

            // Act: Execute the complete command chain
            var spellCastCommand = new SpellCastCommand();
            var token = new CommandCompletionToken();
            CommandResult result = null;

            token.Subscribe((r) => {
                result = r;
            });

            spellCastCommand.Execute(token);

            // Execute the follow-up commands to test the full chain
            var currentState = result.NewState;
            foreach (var command in result.FollowUpCommands.Take(4)) // Execute up to CompleteSpellCommand
            {
                _commandProcessor.SetState(currentState);
                var commandToken = new CommandCompletionToken();
                var commandCompleted = false;
                CommandResult commandResult = null;

                commandToken.Subscribe((r) => {
                    commandCompleted = true;
                    commandResult = r;
                });

                command.Execute(commandToken);

                Assert(commandCompleted, $"Command {command.GetType().Name} should complete");
                Assert(commandResult.IsSuccess, $"Command {command.GetType().Name} should succeed: {commandResult.ErrorMessage}");
                
                currentState = commandResult.NewState;

                // Execute any follow-up commands from this command
                if (commandResult.FollowUpCommands != null)
                {
                    foreach (var followUp in commandResult.FollowUpCommands)
                    {
                        _commandProcessor.SetState(currentState);
                        var followUpToken = new CommandCompletionToken();
                        var followUpCompleted = false;
                        CommandResult followUpResult = null;

                        followUpToken.Subscribe((r) => {
                            followUpCompleted = true;
                            followUpResult = r;
                        });

                        followUp.Execute(followUpToken);

                        if (followUpCompleted && followUpResult.IsSuccess)
                        {
                            currentState = followUpResult.NewState;
                        }
                    }
                }
            }

            // Assert: Verify final state
            Assert(!currentState.Spell.IsActive, "Spell should not be active after completion");
            Assert(currentState.Spell.History.Length > 0, "Spell history should contain completed spell");
            Assert(currentState.Spell.TotalDamageDealt == 0, "Active spell damage should be reset");

            var historyEntry = currentState.Spell.History.Last();
            Assert(historyEntry.WasSuccessful, "Spell should be marked as successful in history");
            Assert(historyEntry.TotalDamage > 0, "History should record damage dealt");

            GD.Print("[SpellProcessingIntegrationTests] TestCommandChainExecution passed");
        }

        private SpellCardResource CreateTestSpellCard(string id, string name, ActionResource[] actions)
        {
            var spellCard = new SpellCardResource();
            spellCard.CardResourceId = id;
            spellCard.CardName = name;
            spellCard.Actions = new Godot.Collections.Array<ActionResource>(actions);
            return spellCard;
        }

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                _logger.LogError($"[SpellProcessingIntegrationTests] Assertion failed: {message}");
                throw new Exception($"Test assertion failed: {message}");
            }
        }

        public void RunAllTests()
        {
            try
            {
                TestCompleteSpellProcessingFlow();
                TestSpellProcessingWithEmptyPlayedCards();
                TestSpellProcessingInWrongPhase();
                TestCommandChainExecution();
                
                GD.Print("[SpellProcessingIntegrationTests] All tests passed!");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SpellProcessingIntegrationTests] Test failed: {ex.Message}");
                throw;
            }
        }
    }
}