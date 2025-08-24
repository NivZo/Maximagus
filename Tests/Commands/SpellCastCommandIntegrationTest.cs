using System;
using System.Linq;
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
    /// Simple integration test for SpellCastCommand to verify the command chain creation
    /// </summary>
    public partial class SpellCastCommandIntegrationTest : RefCounted
    {
        public static void RunTest()
        {
            GD.Print("[SpellCastCommandIntegrationTest] Starting test...");
            
            try
            {
                // Get the command processor from ServiceLocator
                var commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
                
                // Create a simple damage action
                var damageAction = new DamageActionResource
                {
                    DamageType = DamageType.Fire,
                    Amount = 10
                };

                // Create a spell card with the action
                var spellCard = new SpellCardResource();
                spellCard.CardResourceId = "test-card";
                spellCard.CardName = "Test Fire Bolt";
                spellCard.Actions = new Godot.Collections.Array<ActionResource> { damageAction };

                // Create card state in played cards
                var cardState = new CardState("test-card", spellCard, position: 0, containerType: ContainerType.PlayedCards);
                var cardsState = new CardsState(new[] { cardState });
                
                // Create game state in SpellCasting phase
                var phaseState = new GamePhaseState().WithPhase(GamePhase.SpellCasting);
                var gameState = GameState.Create(
                    cardsState,
                    new HandState(),
                    new PlayerState(),
                    phaseState,
                    SpellState.CreateInitial(),
                    StatusEffectsState.CreateInitial()
                );

                // Set the state in the command processor
                commandProcessor.SetState(gameState);

                // Create and test SpellCastCommand
                var spellCastCommand = new SpellCastCommand();

                // Test CanExecute
                var canExecute = spellCastCommand.CanExecute();
                if (!canExecute)
                {
                    throw new Exception("SpellCastCommand should be able to execute");
                }

                // Test Execute
                var token = new CommandCompletionToken();
                var completed = false;
                CommandResult result = null;

                token.Subscribe((r) => {
                    completed = true;
                    result = r;
                });

                spellCastCommand.Execute(token);

                // Verify results
                if (!completed)
                {
                    throw new Exception("SpellCastCommand should complete synchronously");
                }

                if (!result.IsSuccess)
                {
                    throw new Exception($"SpellCastCommand should succeed: {result.ErrorMessage}");
                }

                if (result.FollowUpCommands == null || !result.FollowUpCommands.Any())
                {
                    throw new Exception("SpellCastCommand should create follow-up commands");
                }

                var commands = result.FollowUpCommands.ToArray();
                if (commands.Length < 4)
                {
                    throw new Exception($"Expected at least 4 commands, got {commands.Length}");
                }

                // Verify command types
                if (!(commands[0] is StartSpellCommand))
                {
                    throw new Exception("First command should be StartSpellCommand");
                }

                if (!(commands[1] is ExecuteCardActionCommand))
                {
                    throw new Exception("Second command should be ExecuteCardActionCommand");
                }

                if (!(commands[2] is CompleteSpellCommand))
                {
                    throw new Exception("Third command should be CompleteSpellCommand");
                }

                if (!(commands[3] is TurnEndCommand))
                {
                    throw new Exception("Fourth command should be TurnEndCommand");
                }

                // Verify phase transition
                if (result.NewState.Phase.CurrentPhase != GamePhase.TurnEnd)
                {
                    throw new Exception("Phase should transition to TurnEnd");
                }

                GD.Print("[SpellCastCommandIntegrationTest] ✓ Test passed successfully!");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SpellCastCommandIntegrationTest] ✗ Test failed: {ex.Message}");
                throw;
            }
        }
    }
}