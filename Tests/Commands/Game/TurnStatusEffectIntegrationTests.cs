using System;
using System.Linq;
using Godot;
using Scripts.State;
using Scripts.Config;
using Scripts.Commands;
using Scripts.Commands.Game;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Managers;

namespace Tests.Commands.Game
{
    /// <summary>
    /// Integration tests for status effect processing during turn transitions
    /// </summary>
    public static class TurnStatusEffectIntegrationTests
    {
        public static void RunAllTests()
        {
            GD.Print("=== Running Turn Status Effect Integration Tests ===");
            
            TestTurnStartCommand_TriggersStartOfTurnEffects();
            TestTurnEndCommand_TriggersEndOfTurnEffects();
            TestTurnEndCommand_ProcessesEndOfTurnDecay();
            TestTurnEndCommand_ProcessesReduceByOneDecay();
            TestFullTurnCycle_WithMultipleStatusEffects();
            
            GD.Print("=== All Turn Status Effect Integration Tests Passed ===");
        }

        private static void TestTurnStartCommand_TriggersStartOfTurnEffects()
        {
            // Arrange
            var initialState = CreateGameStateWithStatusEffects();
            var command = new TurnStartCommand();
            
            // Act
            var result = ExecuteCommandAndGetResult(command, initialState);
            
            // Assert
            AssertTrue(result.IsSuccess, "TurnStartCommand should succeed");
            AssertTrue(result.FollowUpCommands.Any(c => c is TriggerStatusEffectsCommand trigger && 
                trigger.GetTrigger() == StatusEffectTrigger.StartOfTurn), 
                "Should include TriggerStatusEffectsCommand for StartOfTurn");
            
            GD.Print("✓ TurnStartCommand_TriggersStartOfTurnEffects");
        }

        private static void TestTurnEndCommand_TriggersEndOfTurnEffects()
        {
            // Arrange
            var initialState = CreateGameStateWithStatusEffects();
            var command = new TurnEndCommand();
            
            // Act
            var result = ExecuteCommandAndGetResult(command, initialState);
            
            // Assert
            AssertTrue(result.IsSuccess, "TurnEndCommand should succeed");
            AssertTrue(result.FollowUpCommands.Any(c => c is TriggerStatusEffectsCommand trigger && 
                trigger.GetTrigger() == StatusEffectTrigger.EndOfTurn), 
                "Should include TriggerStatusEffectsCommand for EndOfTurn");
            
            GD.Print("✓ TurnEndCommand_TriggersEndOfTurnEffects");
        }

        private static void TestTurnEndCommand_ProcessesEndOfTurnDecay()
        {
            // Arrange
            var initialState = CreateGameStateWithStatusEffects();
            var command = new TurnEndCommand();
            
            // Act
            var result = ExecuteCommandAndGetResult(command, initialState);
            
            // Assert
            AssertTrue(result.IsSuccess, "TurnEndCommand should succeed");
            AssertTrue(result.FollowUpCommands.Any(c => c is ProcessStatusEffectDecayCommand decay && 
                decay.GetDecayMode() == StatusEffectDecayMode.EndOfTurn), 
                "Should include ProcessStatusEffectDecayCommand for EndOfTurn");
            
            GD.Print("✓ TurnEndCommand_ProcessesEndOfTurnDecay");
        }

        private static void TestTurnEndCommand_ProcessesReduceByOneDecay()
        {
            // Arrange
            var initialState = CreateGameStateWithStatusEffects();
            var command = new TurnEndCommand();
            
            // Act
            var result = ExecuteCommandAndGetResult(command, initialState);
            
            // Assert
            AssertTrue(result.IsSuccess, "TurnEndCommand should succeed");
            AssertTrue(result.FollowUpCommands.Any(c => c is ProcessStatusEffectDecayCommand decay && 
                decay.GetDecayMode() == StatusEffectDecayMode.ReduceByOneEndOfTurn), 
                "Should include ProcessStatusEffectDecayCommand for ReduceByOneEndOfTurn");
            
            GD.Print("✓ TurnEndCommand_ProcessesReduceByOneDecay");
        }

        private static void TestFullTurnCycle_WithMultipleStatusEffects()
        {
            // Arrange
            var initialState = CreateGameStateWithMultipleStatusEffects();
            var turnEndCommand = new TurnEndCommand();
            
            // Act - Execute turn end
            var turnEndResult = ExecuteCommandAndGetResult(turnEndCommand, initialState);
            
            // Assert turn end commands are correct
            AssertTrue(turnEndResult.IsSuccess, "TurnEndCommand should succeed");
            var followUpArray = turnEndResult.FollowUpCommands.ToArray();
            AssertEqual(4, followUpArray.Length, "Should have 4 follow-up commands");
            
            // Verify command sequence
            AssertTrue(followUpArray[0] is TriggerStatusEffectsCommand, "First should be trigger command");
            AssertTrue(followUpArray[1] is ProcessStatusEffectDecayCommand, "Second should be decay command");
            AssertTrue(followUpArray[2] is ProcessStatusEffectDecayCommand, "Third should be decay command");
            AssertTrue(followUpArray[3] is TurnStartCommand, "Fourth should be turn start command");
            
            GD.Print("✓ TestFullTurnCycle_WithMultipleStatusEffects");
        }

        #region Helper Methods

        private static CommandResult ExecuteCommandAndGetResult(GameCommand command, IGameStateData initialState)
        {
            var mockProcessor = new MockCommandProcessor(initialState);
            ServiceLocator.RegisterService<IGameCommandProcessor>(mockProcessor);
            
            var token = new CommandCompletionToken();
            var result = default(CommandResult);
            
            token.Subscribe((r) => result = r);
            command.Execute(token);
            
            return result;
        }

        private static IGameStateData CreateGameStateWithStatusEffects()
        {
            var initialState = GameState.CreateInitial();
            
            // Add some status effects to test with
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.StartOfTurn, StatusEffectDecayMode.Never);
            var statusEffectsState = StatusEffectLogicManager.ApplyStatusEffect(
                initialState.StatusEffects, 
                poisonResource, 
                2, 
                StatusEffectActionType.Add);
            
            return initialState
                .WithStatusEffects(statusEffectsState)
                .WithPhase(initialState.Phase.WithPhase(GamePhase.TurnEnd));
        }

        private static IGameStateData CreateGameStateWithMultipleStatusEffects()
        {
            var initialState = GameState.CreateInitial();
            
            // Add multiple status effects with different triggers and decay modes
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.EndOfTurn);
            var bleedingResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, StatusEffectTrigger.StartOfTurn, StatusEffectDecayMode.ReduceByOneEndOfTurn);
            var chillResource = CreateTestStatusEffectResource(StatusEffectType.Chill, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            
            var statusEffectsState = initialState.StatusEffects;
            statusEffectsState = StatusEffectLogicManager.ApplyStatusEffect(statusEffectsState, poisonResource, 3, StatusEffectActionType.Add);
            statusEffectsState = StatusEffectLogicManager.ApplyStatusEffect(statusEffectsState, bleedingResource, 2, StatusEffectActionType.Add);
            statusEffectsState = StatusEffectLogicManager.ApplyStatusEffect(statusEffectsState, chillResource, 1, StatusEffectActionType.Add);
            
            return initialState
                .WithStatusEffects(statusEffectsState)
                .WithPhase(initialState.Phase.WithPhase(GamePhase.TurnEnd));
        }

        private static StatusEffectResource CreateTestStatusEffectResource(StatusEffectType effectType, StatusEffectTrigger trigger, StatusEffectDecayMode decayMode)
        {
            var resource = new StatusEffectResource();
            resource.EffectType = effectType;
            resource.Trigger = trigger;
            resource.DecayMode = decayMode;
            resource.EffectName = effectType.ToString();
            resource.Description = $"Test {effectType} effect";
            resource.InitialStacks = 1;
            resource.Value = 1.0f;
            return resource;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
            }
        }

        #endregion

        #region Mock Classes

        private class MockCommandProcessor : IGameCommandProcessor
        {
            public IGameStateData CurrentState { get; private set; }
            public event Action<IGameStateData, IGameStateData> StateChanged;

            public MockCommandProcessor(IGameStateData initialState)
            {
                CurrentState = initialState;
            }

            public bool ExecuteCommand(GameCommand command)
            {
                // Simple mock - just return true
                return true;
            }

            public void SetState(IGameStateData newState)
            {
                var previousState = CurrentState;
                CurrentState = newState;
                StateChanged?.Invoke(previousState, newState);
            }

            public void NotifyBlockingCommandFinished()
            {
                // Mock implementation - do nothing
            }
        }

        #endregion
    }
}