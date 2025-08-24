using System;
using System.Collections.Immutable;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Enums;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for SpellState class
    /// </summary>
    public static class SpellStateTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running SpellState tests...");
            
            TestCreateInitial();
            TestWithActiveSpell();
            TestWithProperties();
            TestWithProperty();
            TestWithModifiers();
            TestWithAddedModifier();
            TestWithTotalDamage();
            TestWithActionIndex();
            TestWithCompletedSpell();
            TestGetProperty();
            TestIsValid();
            TestEquality();
            TestHistoryLimit();
            
            GD.Print("All SpellState tests passed!");
        }

        private static void TestCreateInitial()
        {
            var state = SpellState.CreateInitial();
            
            Assert(state.IsActive == false, "Initial state should not be active");
            Assert(state.Properties.IsEmpty, "Initial state should have empty properties");
            Assert(state.ActiveModifiers.IsEmpty, "Initial state should have empty modifiers");
            Assert(state.TotalDamageDealt == 0f, "Initial state should have zero damage");
            Assert(state.History.IsEmpty, "Initial state should have empty history");
            Assert(state.StartTime == null, "Initial state should have no start time");
            Assert(state.CurrentActionIndex == 0, "Initial state should have zero action index");
            Assert(state.IsValid(), "Initial state should be valid");
        }

        private static void TestWithActiveSpell()
        {
            var initialState = SpellState.CreateInitial();
            var startTime = DateTime.UtcNow;
            var activeState = initialState.WithActiveSpell(startTime);
            
            Assert(activeState.IsActive == true, "Active spell state should be active");
            Assert(activeState.StartTime == startTime, "Active spell should have correct start time");
            Assert(activeState.Properties.IsEmpty, "Active spell should start with empty properties");
            Assert(activeState.ActiveModifiers.IsEmpty, "Active spell should start with empty modifiers");
            Assert(activeState.TotalDamageDealt == 0f, "Active spell should start with zero damage");
            Assert(activeState.CurrentActionIndex == 0, "Active spell should start with zero action index");
            Assert(activeState.IsValid(), "Active spell state should be valid");
        }

        private static void TestWithProperties()
        {
            var state = SpellState.CreateInitial();
            var properties = ImmutableDictionary<string, Variant>.Empty
                .Add("FireDamageDealt", Variant.From(10f))
                .Add("FrostDamageDealt", Variant.From(5f));
            
            var newState = state.WithProperties(properties);
            
            Assert(newState.Properties.Count == 2, "State should have 2 properties");
            Assert(newState.GetProperty("FireDamageDealt", 0f) == 10f, "Fire damage should be 10");
            Assert(newState.GetProperty("FrostDamageDealt", 0f) == 5f, "Frost damage should be 5");
            Assert(newState.IsValid(), "State with properties should be valid");
        }

        private static void TestWithProperty()
        {
            var state = SpellState.CreateInitial();
            var newState = state.WithProperty("TestProperty", Variant.From(42));
            
            Assert(newState.Properties.Count == 1, "State should have 1 property");
            Assert(newState.GetProperty("TestProperty", 0) == 42, "Test property should be 42");
            Assert(newState.IsValid(), "State with single property should be valid");
        }

        private static void TestWithModifiers()
        {
            var state = SpellState.CreateInitial();
            var modifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true);
            var modifiers = ImmutableArray.Create(modifier);
            
            var newState = state.WithModifiers(modifiers);
            
            Assert(newState.ActiveModifiers.Length == 1, "State should have 1 modifier");
            Assert(newState.ActiveModifiers[0].Equals(modifier), "Modifier should match");
            Assert(newState.IsValid(), "State with modifiers should be valid");
        }

        private static void TestWithAddedModifier()
        {
            var state = SpellState.CreateInitial();
            var modifier1 = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true);
            var modifier2 = new ModifierData(ModifierType.Multiply, DamageType.Frost, 2f, false);
            
            var newState = state.WithAddedModifier(modifier1).WithAddedModifier(modifier2);
            
            Assert(newState.ActiveModifiers.Length == 2, "State should have 2 modifiers");
            Assert(newState.ActiveModifiers[0].Equals(modifier1), "First modifier should match");
            Assert(newState.ActiveModifiers[1].Equals(modifier2), "Second modifier should match");
            Assert(newState.IsValid(), "State with added modifiers should be valid");
        }

        private static void TestWithTotalDamage()
        {
            var state = SpellState.CreateInitial();
            var newState = state.WithTotalDamage(25.5f);
            
            Assert(Math.Abs(newState.TotalDamageDealt - 25.5f) < 0.001f, "Total damage should be 25.5");
            Assert(newState.IsValid(), "State with damage should be valid");
        }

        private static void TestWithActionIndex()
        {
            var state = SpellState.CreateInitial();
            var newState = state.WithActionIndex(3);
            
            Assert(newState.CurrentActionIndex == 3, "Action index should be 3");
            Assert(newState.IsValid(), "State with action index should be valid");
        }

        private static void TestWithCompletedSpell()
        {
            var activeState = SpellState.CreateInitial()
                .WithActiveSpell(DateTime.UtcNow)
                .WithTotalDamage(15f)
                .WithProperty("TestProp", Variant.From(100));
            
            var historyEntry = SpellHistoryEntry.CreateSuccessful(
                activeState,
                ImmutableArray.Create("card1", "card2"),
                ImmutableArray<Maximagus.Scripts.Spells.Abstractions.SpellCardResource>.Empty);
            
            var completedState = activeState.WithCompletedSpell(historyEntry);
            
            Assert(completedState.IsActive == false, "Completed state should not be active");
            Assert(completedState.Properties.IsEmpty, "Completed state should have empty properties");
            Assert(completedState.ActiveModifiers.IsEmpty, "Completed state should have empty modifiers");
            Assert(completedState.TotalDamageDealt == 0f, "Completed state should have zero damage");
            Assert(completedState.StartTime == null, "Completed state should have no start time");
            Assert(completedState.CurrentActionIndex == 0, "Completed state should have zero action index");
            Assert(completedState.History.Length == 1, "Completed state should have 1 history entry");
            Assert(completedState.IsValid(), "Completed state should be valid");
        }

        private static void TestGetProperty()
        {
            var state = SpellState.CreateInitial()
                .WithProperty("IntProp", Variant.From(42))
                .WithProperty("FloatProp", Variant.From(3.14f))
                .WithProperty("StringProp", Variant.From("test"));
            
            Assert(state.GetProperty("IntProp", 0) == 42, "Int property should be 42");
            Assert(Math.Abs(state.GetProperty("FloatProp", 0f) - 3.14f) < 0.001f, "Float property should be 3.14");
            Assert(state.GetProperty("StringProp", "") == "test", "String property should be 'test'");
            Assert(state.GetProperty("NonExistent", 99) == 99, "Non-existent property should return default");
        }

        private static void TestIsValid()
        {
            // Valid states
            var validInitial = SpellState.CreateInitial();
            Assert(validInitial.IsValid(), "Initial state should be valid");
            
            var validActive = validInitial.WithActiveSpell(DateTime.UtcNow);
            Assert(validActive.IsValid(), "Active state should be valid");
            
            // Invalid states would need to be created through reflection or internal constructors
            // For now, we test the validation logic with edge cases
            var stateWithDamage = validInitial.WithTotalDamage(100f);
            Assert(stateWithDamage.IsValid(), "State with positive damage should be valid");
        }

        private static void TestEquality()
        {
            var state1 = SpellState.CreateInitial().WithProperty("test", Variant.From(42));
            var state2 = SpellState.CreateInitial().WithProperty("test", Variant.From(42));
            var state3 = SpellState.CreateInitial().WithProperty("test", Variant.From(43));
            
            Assert(state1.Equals(state2), "States with same properties should be equal");
            Assert(!state1.Equals(state3), "States with different properties should not be equal");
            Assert(!state1.Equals(null), "State should not equal null");
            Assert(!state1.Equals("string"), "State should not equal different type");
        }

        private static void TestHistoryLimit()
        {
            var state = SpellState.CreateInitial();
            
            // Add more than 50 history entries
            for (int i = 0; i < 55; i++)
            {
                var activeState = state.WithActiveSpell(DateTime.UtcNow).WithTotalDamage(i);
                var historyEntry = SpellHistoryEntry.CreateSuccessful(
                    activeState,
                    ImmutableArray.Create($"card{i}"),
                    ImmutableArray<Maximagus.Scripts.Spells.Abstractions.SpellCardResource>.Empty);
                
                state = state.WithCompletedSpell(historyEntry);
            }
            
            Assert(state.History.Length == 50, "History should be limited to 50 entries");
            Assert(state.IsValid(), "State with limited history should be valid");
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