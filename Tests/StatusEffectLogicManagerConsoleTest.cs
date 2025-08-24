using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;

namespace Tests
{
    /// <summary>
    /// Console-based test runner for StatusEffectLogicManager that doesn't require Godot
    /// </summary>
    public class StatusEffectLogicManagerConsoleTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== StatusEffectLogicManager Console Tests ===");
            
            try
            {
                RunAllTests();
                Console.WriteLine("✅ All StatusEffectLogicManager tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private static void RunAllTests()
        {
            TestApplyStatusEffect();
            TestTriggerEffects();
            TestProcessDecay();
            TestGetStacksOfEffect();
            TestHasEffect();
            TestGetEffectsForTrigger();
            TestGetEffect();
            TestRemoveExpiredEffects();
            TestValidateState();
            TestNullArgumentHandling();
        }

        private static void TestApplyStatusEffect()
        {
            Console.WriteLine("Testing ApplyStatusEffect...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.ReduceByOneEndOfTurn);
            var bleedingResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, StatusEffectTrigger.OnDamageDealt, StatusEffectDecayMode.Never);
            
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test adding new effect
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            Assert(state1.GetStacksOfEffect(StatusEffectType.Poison) == 3, "Should add 3 poison stacks");
            Assert(state1.TotalActiveEffects == 1, "Should have 1 active effect");
            
            // Test adding to existing effect
            var state2 = StatusEffectLogicManager.ApplyStatusEffect(state1, poisonResource, 2, StatusEffectActionType.Add);
            Assert(state2.GetStacksOfEffect(StatusEffectType.Poison) == 5, "Should have 5 poison stacks (3 + 2)");
            Assert(state2.TotalActiveEffects == 1, "Should still have 1 active effect");
            
            // Test setting existing effect
            var state3 = StatusEffectLogicManager.ApplyStatusEffect(state2, poisonResource, 1, StatusEffectActionType.Set);
            Assert(state3.GetStacksOfEffect(StatusEffectType.Poison) == 1, "Should have 1 poison stack (set to 1)");
            
            // Test removing from existing effect
            var state4 = StatusEffectLogicManager.ApplyStatusEffect(state3, poisonResource, 1, StatusEffectActionType.Remove);
            Assert(state4.TotalActiveEffects == 0, "Should remove effect when stacks reach 0");
            
            // Test adding different effect type
            var state5 = StatusEffectLogicManager.ApplyStatusEffect(state1, bleedingResource, 2, StatusEffectActionType.Add);
            Assert(state5.TotalActiveEffects == 2, "Should have 2 different active effects");
            Assert(state5.GetStacksOfEffect(StatusEffectType.Poison) == 3, "Should still have 3 poison stacks");
            Assert(state5.GetStacksOfEffect(StatusEffectType.Bleeding) == 2, "Should have 2 bleeding stacks");
            
            Console.WriteLine("✅ ApplyStatusEffect tests passed");
        }

        private static void TestTriggerEffects()
        {
            Console.WriteLine("Testing TriggerEffects...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.ReduceByOneOnTrigger);
            var bleedingResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, StatusEffectTrigger.OnDamageDealt, StatusEffectDecayMode.RemoveOnTrigger);
            var chillResource = CreateTestStatusEffectResource(StatusEffectType.Chill, StatusEffectTrigger.StartOfTurn, StatusEffectDecayMode.Never);
            
            var initialState = StatusEffectsState.CreateInitial();
            
            // Add multiple effects
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            var state2 = StatusEffectLogicManager.ApplyStatusEffect(state1, bleedingResource, 2, StatusEffectActionType.Add);
            var state3 = StatusEffectLogicManager.ApplyStatusEffect(state2, chillResource, 1, StatusEffectActionType.Add);
            
            Assert(state3.TotalActiveEffects == 3, "Should have 3 active effects");
            
            // Test triggering EndOfTurn effects (poison should reduce by 1)
            var state4 = StatusEffectLogicManager.TriggerEffects(state3, StatusEffectTrigger.EndOfTurn);
            Assert(state4.GetStacksOfEffect(StatusEffectType.Poison) == 2, "Poison should reduce by 1 stack");
            Assert(state4.GetStacksOfEffect(StatusEffectType.Bleeding) == 2, "Bleeding should remain unchanged");
            Assert(state4.GetStacksOfEffect(StatusEffectType.Chill) == 1, "Chill should remain unchanged");
            
            // Test triggering OnDamageDealt effects (bleeding should be removed)
            var state5 = StatusEffectLogicManager.TriggerEffects(state4, StatusEffectTrigger.OnDamageDealt);
            Assert(state5.GetStacksOfEffect(StatusEffectType.Bleeding) == 0, "Bleeding should be removed");
            Assert(state5.TotalActiveEffects == 2, "Should have 2 active effects after bleeding removal");
            
            Console.WriteLine("✅ TriggerEffects tests passed");
        }

        private static void TestProcessDecay()
        {
            Console.WriteLine("Testing ProcessDecay...");
            
            var endOfTurnResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.EndOfTurn);
            var reduceByOneResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, StatusEffectTrigger.OnDamageDealt, StatusEffectDecayMode.ReduceByOneEndOfTurn);
            var neverDecayResource = CreateTestStatusEffectResource(StatusEffectType.Chill, StatusEffectTrigger.StartOfTurn, StatusEffectDecayMode.Never);
            
            var initialState = StatusEffectsState.CreateInitial();
            
            // Add multiple effects
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, endOfTurnResource, 3, StatusEffectActionType.Add);
            var state2 = StatusEffectLogicManager.ApplyStatusEffect(state1, reduceByOneResource, 2, StatusEffectActionType.Add);
            var state3 = StatusEffectLogicManager.ApplyStatusEffect(state2, neverDecayResource, 1, StatusEffectActionType.Add);
            
            Assert(state3.TotalActiveEffects == 3, "Should have 3 active effects");
            
            // Test EndOfTurn decay (poison should be removed completely)
            var state4 = StatusEffectLogicManager.ProcessDecay(state3, StatusEffectDecayMode.EndOfTurn);
            Assert(state4.GetStacksOfEffect(StatusEffectType.Poison) == 0, "Poison should be removed");
            Assert(state4.GetStacksOfEffect(StatusEffectType.Bleeding) == 2, "Bleeding should remain unchanged");
            Assert(state4.GetStacksOfEffect(StatusEffectType.Chill) == 1, "Chill should remain unchanged");
            
            // Test ReduceByOneEndOfTurn decay (bleeding should reduce by 1)
            var state5 = StatusEffectLogicManager.ProcessDecay(state4, StatusEffectDecayMode.ReduceByOneEndOfTurn);
            Assert(state5.GetStacksOfEffect(StatusEffectType.Bleeding) == 1, "Bleeding should reduce by 1");
            Assert(state5.GetStacksOfEffect(StatusEffectType.Chill) == 1, "Chill should remain unchanged");
            
            Console.WriteLine("✅ ProcessDecay tests passed");
        }

        private static void TestGetStacksOfEffect()
        {
            Console.WriteLine("Testing GetStacksOfEffect...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test empty state
            Assert(StatusEffectLogicManager.GetStacksOfEffect(initialState, StatusEffectType.Poison) == 0, "Should return 0 for empty state");
            
            // Test with effect present
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 5, StatusEffectActionType.Add);
            Assert(StatusEffectLogicManager.GetStacksOfEffect(state1, StatusEffectType.Poison) == 5, "Should return 5 poison stacks");
            
            // Test with non-existent effect
            Assert(StatusEffectLogicManager.GetStacksOfEffect(state1, StatusEffectType.Bleeding) == 0, "Should return 0 for non-existent effect");
            
            Console.WriteLine("✅ GetStacksOfEffect tests passed");
        }

        private static void TestHasEffect()
        {
            Console.WriteLine("Testing HasEffect...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test empty state
            Assert(!StatusEffectLogicManager.HasEffect(initialState, StatusEffectType.Poison), "Should return false for empty state");
            
            // Test with effect present
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 1, StatusEffectActionType.Add);
            Assert(StatusEffectLogicManager.HasEffect(state1, StatusEffectType.Poison), "Should return true when effect is present");
            
            // Test with non-existent effect
            Assert(!StatusEffectLogicManager.HasEffect(state1, StatusEffectType.Bleeding), "Should return false for non-existent effect");
            
            Console.WriteLine("✅ HasEffect tests passed");
        }

        private static void TestGetEffectsForTrigger()
        {
            Console.WriteLine("Testing GetEffectsForTrigger...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var bleedingResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, StatusEffectTrigger.OnDamageDealt, StatusEffectDecayMode.Never);
            var chillResource = CreateTestStatusEffectResource(StatusEffectType.Chill, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            
            var initialState = StatusEffectsState.CreateInitial();
            
            // Add multiple effects
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 1, StatusEffectActionType.Add);
            var state2 = StatusEffectLogicManager.ApplyStatusEffect(state1, bleedingResource, 1, StatusEffectActionType.Add);
            var state3 = StatusEffectLogicManager.ApplyStatusEffect(state2, chillResource, 1, StatusEffectActionType.Add);
            
            // Test getting EndOfTurn effects (should get poison and chill)
            var endOfTurnEffects = StatusEffectLogicManager.GetEffectsForTrigger(state3, StatusEffectTrigger.EndOfTurn);
            Assert(endOfTurnEffects.Length == 2, "Should have 2 EndOfTurn effects");
            Assert(endOfTurnEffects.Any(e => e.EffectType == StatusEffectType.Poison), "Should include poison");
            Assert(endOfTurnEffects.Any(e => e.EffectType == StatusEffectType.Chill), "Should include chill");
            
            // Test getting OnDamageDealt effects (should get bleeding only)
            var onDamageEffects = StatusEffectLogicManager.GetEffectsForTrigger(state3, StatusEffectTrigger.OnDamageDealt);
            Assert(onDamageEffects.Length == 1, "Should have 1 OnDamageDealt effect");
            Assert(onDamageEffects[0].EffectType == StatusEffectType.Bleeding, "Should be bleeding effect");
            
            Console.WriteLine("✅ GetEffectsForTrigger tests passed");
        }

        private static void TestGetEffect()
        {
            Console.WriteLine("Testing GetEffect...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test empty state
            var emptyResult = StatusEffectLogicManager.GetEffect(initialState, StatusEffectType.Poison);
            Assert(emptyResult == null, "Should return null for empty state");
            
            // Test with effect present
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            var effect = StatusEffectLogicManager.GetEffect(state1, StatusEffectType.Poison);
            Assert(effect != null, "Should return effect when present");
            Assert(effect.EffectType == StatusEffectType.Poison, "Should return correct effect type");
            Assert(effect.CurrentStacks == 3, "Should return correct stack count");
            
            Console.WriteLine("✅ GetEffect tests passed");
        }

        private static void TestRemoveExpiredEffects()
        {
            Console.WriteLine("Testing RemoveExpiredEffects...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var initialState = StatusEffectsState.CreateInitial();
            
            // Add effect and then set it to 0 stacks (expired)
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            var state2 = StatusEffectLogicManager.ApplyStatusEffect(state1, poisonResource, 0, StatusEffectActionType.Set);
            
            // The WithAppliedEffect should automatically remove expired effects, so this should already be clean
            Assert(state2.TotalActiveEffects == 0, "Should automatically remove expired effects");
            
            // Test RemoveExpiredEffects on a clean state
            var cleanedState = StatusEffectLogicManager.RemoveExpiredEffects(state2);
            Assert(cleanedState.TotalActiveEffects == 0, "Should remain clean");
            
            Console.WriteLine("✅ RemoveExpiredEffects tests passed");
        }

        private static void TestValidateState()
        {
            Console.WriteLine("Testing ValidateState...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test valid empty state
            Assert(StatusEffectLogicManager.ValidateState(initialState), "Empty state should be valid");
            
            // Test valid state with effects
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            Assert(StatusEffectLogicManager.ValidateState(state1), "State with valid effects should be valid");
            
            // Test null state
            Assert(!StatusEffectLogicManager.ValidateState(null), "Null state should be invalid");
            
            Console.WriteLine("✅ ValidateState tests passed");
        }

        private static void TestNullArgumentHandling()
        {
            Console.WriteLine("Testing null argument handling...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, StatusEffectTrigger.EndOfTurn, StatusEffectDecayMode.Never);
            var validState = StatusEffectsState.CreateInitial();
            
            // Test ApplyStatusEffect with null arguments
            try
            {
                StatusEffectLogicManager.ApplyStatusEffect(null, poisonResource, 1, StatusEffectActionType.Add);
                Assert(false, "Should throw exception for null state");
            }
            catch (ArgumentNullException) { /* Expected */ }
            
            try
            {
                StatusEffectLogicManager.ApplyStatusEffect(validState, null, 1, StatusEffectActionType.Add);
                Assert(false, "Should throw exception for null effect");
            }
            catch (ArgumentNullException) { /* Expected */ }
            
            // Test other methods with null state
            try
            {
                StatusEffectLogicManager.TriggerEffects(null, StatusEffectTrigger.EndOfTurn);
                Assert(false, "Should throw exception for null state");
            }
            catch (ArgumentNullException) { /* Expected */ }
            
            try
            {
                StatusEffectLogicManager.ProcessDecay(null, StatusEffectDecayMode.EndOfTurn);
                Assert(false, "Should throw exception for null state");
            }
            catch (ArgumentNullException) { /* Expected */ }
            
            try
            {
                StatusEffectLogicManager.GetStacksOfEffect(null, StatusEffectType.Poison);
                Assert(false, "Should throw exception for null state");
            }
            catch (ArgumentNullException) { /* Expected */ }
            
            Console.WriteLine("✅ Null argument handling tests passed");
        }

        private static StatusEffectResource CreateTestStatusEffectResource(
            StatusEffectType effectType, 
            StatusEffectTrigger trigger, 
            StatusEffectDecayMode decayMode,
            int maxStacks = 10,
            float value = 1.0f)
        {
            var resource = new StatusEffectResource();
            resource.EffectType = effectType;
            resource.EffectName = effectType.ToString();
            resource.Description = $"Test {effectType} effect";
            resource.Trigger = trigger;
            resource.DecayMode = decayMode;
            resource.InitialStacks = 1;
            resource.Value = value;
            resource.MaxStacks = maxStacks;
            return resource;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}