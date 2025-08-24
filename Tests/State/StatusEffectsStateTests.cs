using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for StatusEffectsState class
    /// </summary>
    public static class StatusEffectsStateTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running StatusEffectsState tests...");
            
            TestCreateInitial();
            TestConstructor();
            TestWithActiveEffects();
            TestWithAddedEffect();
            TestWithAppliedEffect();
            TestWithRemovedEffect();
            TestWithExpiredEffectsRemoved();
            TestWithDecayProcessed();
            TestGetEffectsForTrigger();
            TestGetStacksOfEffect();
            TestGetEffect();
            TestHasEffect();
            TestProperties();
            TestIsValid();
            TestEquality();
            TestToString();
            
            GD.Print("All StatusEffectsState tests passed!");
        }

        private static void TestCreateInitial()
        {
            var state = StatusEffectsState.CreateInitial();
            
            Assert(state.ActiveEffects.IsEmpty, "Initial state should have no active effects");
            Assert(state.TotalActiveEffects == 0, "Initial state should have 0 total effects");
            Assert(!state.HasAnyActiveEffects, "Initial state should not have any active effects");
            Assert(state.IsValid(), "Initial state should be valid");
        }

        private static void TestConstructor()
        {
            // Test default constructor
            var state1 = new StatusEffectsState();
            Assert(state1.ActiveEffects.IsEmpty, "Default constructor should create empty effects");
            
            // Test constructor with effects
            var effect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var effects = ImmutableArray.Create(effect);
            var state2 = new StatusEffectsState(effects);
            Assert(state2.ActiveEffects.Length == 1, "Constructor should set active effects");
            Assert(state2.ActiveEffects[0].Equals(effect), "Effect should match");
        }

        private static void TestWithActiveEffects()
        {
            var state = StatusEffectsState.CreateInitial();
            var effect1 = CreateTestStatusEffectInstance(StatusEffectType.Poison, 2);
            var effect2 = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 1);
            var effects = ImmutableArray.Create(effect1, effect2);
            
            var newState = state.WithActiveEffects(effects);
            
            Assert(newState.ActiveEffects.Length == 2, "State should have 2 effects");
            Assert(newState.ActiveEffects[0].Equals(effect1), "First effect should match");
            Assert(newState.ActiveEffects[1].Equals(effect2), "Second effect should match");
            Assert(newState.IsValid(), "State with effects should be valid");
        }

        private static void TestWithAddedEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var effect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            
            var newState = state.WithAddedEffect(effect);
            
            Assert(newState.ActiveEffects.Length == 1, "State should have 1 effect");
            Assert(newState.ActiveEffects[0].Equals(effect), "Effect should match");
            
            // Test null effect throws exception
            try
            {
                state.WithAddedEffect(null);
                Assert(false, "Should throw exception for null effect");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithAppliedEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var resource = CreateTestStatusEffectResource(StatusEffectType.Poison);
            
            // Test adding new effect
            var newState1 = state.WithAppliedEffect(resource, 3, StatusEffectActionType.Add);
            Assert(newState1.ActiveEffects.Length == 1, "Should add new effect");
            Assert(newState1.GetStacksOfEffect(StatusEffectType.Poison) == 3, "Should have 3 poison stacks");
            
            // Test adding to existing effect
            var newState2 = newState1.WithAppliedEffect(resource, 2, StatusEffectActionType.Add);
            Assert(newState2.ActiveEffects.Length == 1, "Should still have 1 effect");
            Assert(newState2.GetStacksOfEffect(StatusEffectType.Poison) == 5, "Should have 5 poison stacks (3 + 2)");
            
            // Test setting existing effect
            var newState3 = newState2.WithAppliedEffect(resource, 1, StatusEffectActionType.Set);
            Assert(newState3.GetStacksOfEffect(StatusEffectType.Poison) == 1, "Should have 1 poison stack (set to 1)");
            
            // Test removing from existing effect
            var newState4 = newState3.WithAppliedEffect(resource, 1, StatusEffectActionType.Remove);
            Assert(newState4.ActiveEffects.Length == 0, "Should remove effect when stacks reach 0");
            
            // Test removing non-existent effect
            var newState5 = state.WithAppliedEffect(resource, 1, StatusEffectActionType.Remove);
            Assert(newState5.ActiveEffects.Length == 0, "Should remain empty when removing non-existent effect");
            
            // Test null resource throws exception
            try
            {
                state.WithAppliedEffect(null, 1, StatusEffectActionType.Add);
                Assert(false, "Should throw exception for null resource");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithRemovedEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var bleedingEffect = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 2);
            
            var stateWithEffects = state
                .WithAddedEffect(poisonEffect)
                .WithAddedEffect(bleedingEffect);
            
            // Test removing existing effect
            var newState1 = stateWithEffects.WithRemovedEffect(StatusEffectType.Poison);
            Assert(newState1.ActiveEffects.Length == 1, "Should have 1 effect remaining");
            Assert(!newState1.HasEffect(StatusEffectType.Poison), "Should not have poison");
            Assert(newState1.HasEffect(StatusEffectType.Bleeding), "Should still have bleeding");
            
            // Test removing non-existent effect
            var newState2 = newState1.WithRemovedEffect(StatusEffectType.Chill);
            Assert(newState2.ActiveEffects.Length == 1, "Should still have 1 effect");
            Assert(newState2.HasEffect(StatusEffectType.Bleeding), "Should still have bleeding");
        }

        private static void TestWithExpiredEffectsRemoved()
        {
            var state = StatusEffectsState.CreateInitial();
            var activeEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var expiredEffect = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 0);
            
            var stateWithEffects = state
                .WithAddedEffect(activeEffect)
                .WithAddedEffect(expiredEffect);
            
            var cleanedState = stateWithEffects.WithExpiredEffectsRemoved();
            
            Assert(cleanedState.ActiveEffects.Length == 1, "Should have 1 effect remaining");
            Assert(cleanedState.HasEffect(StatusEffectType.Poison), "Should keep active effect");
            Assert(!cleanedState.HasEffect(StatusEffectType.Bleeding), "Should remove expired effect");
        }

        private static void TestWithDecayProcessed()
        {
            var state = StatusEffectsState.CreateInitial();
            
            // Create effects with different decay modes
            var removeOnTriggerEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3, StatusEffectDecayMode.RemoveOnTrigger);
            var reduceOnTriggerEffect = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 3, StatusEffectDecayMode.ReduceByOneOnTrigger);
            var endOfTurnEffect = CreateTestStatusEffectInstance(StatusEffectType.Chill, 3, StatusEffectDecayMode.EndOfTurn);
            var neverDecayEffect = CreateTestStatusEffectInstance(StatusEffectType.Burning, 3, StatusEffectDecayMode.Never);
            
            var stateWithEffects = state
                .WithAddedEffect(removeOnTriggerEffect)
                .WithAddedEffect(reduceOnTriggerEffect)
                .WithAddedEffect(endOfTurnEffect)
                .WithAddedEffect(neverDecayEffect);
            
            // Test RemoveOnTrigger decay
            var decayedState1 = stateWithEffects.WithDecayProcessed(StatusEffectDecayMode.RemoveOnTrigger);
            Assert(!decayedState1.HasEffect(StatusEffectType.Poison), "RemoveOnTrigger effect should be removed");
            Assert(decayedState1.HasEffect(StatusEffectType.Bleeding), "Other effects should remain");
            
            // Test ReduceByOneOnTrigger decay
            var decayedState2 = stateWithEffects.WithDecayProcessed(StatusEffectDecayMode.ReduceByOneOnTrigger);
            Assert(decayedState2.GetStacksOfEffect(StatusEffectType.Bleeding) == 2, "ReduceByOne effect should have 2 stacks (3-1)");
            Assert(decayedState2.HasEffect(StatusEffectType.Poison), "Other effects should remain unchanged");
            
            // Test EndOfTurn decay
            var decayedState3 = stateWithEffects.WithDecayProcessed(StatusEffectDecayMode.EndOfTurn);
            Assert(!decayedState3.HasEffect(StatusEffectType.Chill), "EndOfTurn effect should be removed");
            Assert(decayedState3.HasEffect(StatusEffectType.Burning), "Never decay effect should remain");
        }

        private static void TestGetEffectsForTrigger()
        {
            var state = StatusEffectsState.CreateInitial();
            var startOfTurnEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3, StatusEffectDecayMode.Never, StatusEffectTrigger.StartOfTurn);
            var endOfTurnEffect = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 2, StatusEffectDecayMode.Never, StatusEffectTrigger.EndOfTurn);
            var onDamageEffect = CreateTestStatusEffectInstance(StatusEffectType.Chill, 1, StatusEffectDecayMode.Never, StatusEffectTrigger.OnDamageDealt);
            
            var stateWithEffects = state
                .WithAddedEffect(startOfTurnEffect)
                .WithAddedEffect(endOfTurnEffect)
                .WithAddedEffect(onDamageEffect);
            
            var startOfTurnEffects = stateWithEffects.GetEffectsForTrigger(StatusEffectTrigger.StartOfTurn);
            Assert(startOfTurnEffects.Length == 1, "Should have 1 start of turn effect");
            Assert(startOfTurnEffects[0].EffectType == StatusEffectType.Poison, "Should be poison effect");
            
            var endOfTurnEffects = stateWithEffects.GetEffectsForTrigger(StatusEffectTrigger.EndOfTurn);
            Assert(endOfTurnEffects.Length == 1, "Should have 1 end of turn effect");
            Assert(endOfTurnEffects[0].EffectType == StatusEffectType.Bleeding, "Should be bleeding effect");
            
            var onSpellCastEffects = stateWithEffects.GetEffectsForTrigger(StatusEffectTrigger.OnSpellCast);
            Assert(onSpellCastEffects.Length == 0, "Should have no spell cast effects");
        }

        private static void TestGetStacksOfEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 5);
            
            var stateWithEffect = state.WithAddedEffect(poisonEffect);
            
            Assert(stateWithEffect.GetStacksOfEffect(StatusEffectType.Poison) == 5, "Should return 5 poison stacks");
            Assert(stateWithEffect.GetStacksOfEffect(StatusEffectType.Bleeding) == 0, "Should return 0 for non-existent effect");
        }

        private static void TestGetEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            
            var stateWithEffect = state.WithAddedEffect(poisonEffect);
            
            var retrievedEffect = stateWithEffect.GetEffect(StatusEffectType.Poison);
            Assert(retrievedEffect != null, "Should return poison effect");
            Assert(retrievedEffect.Equals(poisonEffect), "Retrieved effect should match original");
            
            var nonExistentEffect = stateWithEffect.GetEffect(StatusEffectType.Bleeding);
            Assert(nonExistentEffect == null, "Should return null for non-existent effect");
        }

        private static void TestHasEffect()
        {
            var state = StatusEffectsState.CreateInitial();
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            
            var stateWithEffect = state.WithAddedEffect(poisonEffect);
            
            Assert(stateWithEffect.HasEffect(StatusEffectType.Poison), "Should have poison effect");
            Assert(!stateWithEffect.HasEffect(StatusEffectType.Bleeding), "Should not have bleeding effect");
        }

        private static void TestProperties()
        {
            var state = StatusEffectsState.CreateInitial();
            
            Assert(state.TotalActiveEffects == 0, "Initial state should have 0 total effects");
            Assert(!state.HasAnyActiveEffects, "Initial state should not have any active effects");
            
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var bleedingEffect = CreateTestStatusEffectInstance(StatusEffectType.Bleeding, 2);
            
            var stateWithEffects = state
                .WithAddedEffect(poisonEffect)
                .WithAddedEffect(bleedingEffect);
            
            Assert(stateWithEffects.TotalActiveEffects == 2, "State should have 2 total effects");
            Assert(stateWithEffects.HasAnyActiveEffects, "State should have active effects");
        }

        private static void TestIsValid()
        {
            var state = StatusEffectsState.CreateInitial();
            Assert(state.IsValid(), "Initial state should be valid");
            
            var validEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var stateWithValidEffect = state.WithAddedEffect(validEffect);
            Assert(stateWithValidEffect.IsValid(), "State with valid effect should be valid");
            
            // Test with duplicate effect types (would need to be created manually to test validation)
            // This is difficult to test without creating invalid state through reflection
        }

        private static void TestEquality()
        {
            var state1 = StatusEffectsState.CreateInitial();
            var state2 = StatusEffectsState.CreateInitial();
            
            Assert(state1.Equals(state2), "Empty states should be equal");
            
            var effect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var stateWithEffect1 = state1.WithAddedEffect(effect);
            var stateWithEffect2 = state2.WithAddedEffect(effect);
            
            Assert(stateWithEffect1.Equals(stateWithEffect2), "States with same effects should be equal");
            Assert(!state1.Equals(stateWithEffect1), "Empty and non-empty states should not be equal");
            Assert(!state1.Equals(null), "State should not equal null");
            Assert(!state1.Equals("string"), "State should not equal different type");
        }

        private static void TestToString()
        {
            var emptyState = StatusEffectsState.CreateInitial();
            var emptyStr = emptyState.ToString();
            Assert(emptyStr.Contains("No active effects"), "Empty state toString should indicate no effects");
            
            var poisonEffect = CreateTestStatusEffectInstance(StatusEffectType.Poison, 3);
            var stateWithEffect = emptyState.WithAddedEffect(poisonEffect);
            var effectStr = stateWithEffect.ToString();
            Assert(effectStr.Contains("Poison"), "ToString should contain effect type");
            Assert(effectStr.Contains("x3"), "ToString should contain stack count");
        }

        private static StatusEffectInstanceData CreateTestStatusEffectInstance(
            StatusEffectType effectType, 
            int stacks, 
            StatusEffectDecayMode decayMode = StatusEffectDecayMode.Never,
            StatusEffectTrigger trigger = StatusEffectTrigger.StartOfTurn)
        {
            var resource = CreateTestStatusEffectResource(effectType, decayMode, trigger);
            return StatusEffectInstanceData.FromResource(resource, stacks);
        }

        private static StatusEffectResource CreateTestStatusEffectResource(
            StatusEffectType effectType,
            StatusEffectDecayMode decayMode = StatusEffectDecayMode.Never,
            StatusEffectTrigger trigger = StatusEffectTrigger.StartOfTurn)
        {
            var resource = new StatusEffectResource();
            resource.EffectType = effectType;
            resource.EffectName = $"Test {effectType}";
            resource.Description = $"Test {effectType} effect";
            resource.Trigger = trigger;
            resource.DecayMode = decayMode;
            resource.InitialStacks = 1;
            resource.Value = 2.0f;
            resource.MaxStacks = 10;
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