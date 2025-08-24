using System;
using System.Collections.Immutable;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Enums;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for GameState class
    /// </summary>
    public static class GameStateTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running GameState tests...");
            
            TestCreateInitial();
            TestCreateInitialSpellState();
            TestCreateInitialStatusEffectsState();
            TestCreateWithAllParameters();
            TestWithSpell();
            TestWithSpellNull();
            TestWithStatusEffects();
            TestWithStatusEffectsNull();
            TestWithComponents();
            TestIsValidWithValidStates();
            TestIsValidWithInvalidSpellState();
            TestGetStateSummary();
            TestDeepCopy();
            TestEquality();
            TestEqualityWithDifferentSpellStates();
            TestEqualityWithDifferentStatusEffectsStates();
            TestToString();
            TestConstructorWithNullSpellState();
            TestConstructorWithNullStatusEffectsState();
            TestWithCardsPreservesSpellAndStatusEffects();
            TestWithHandPreservesSpellAndStatusEffects();
            TestWithPlayerPreservesSpellAndStatusEffects();
            TestWithPhasePreservesSpellAndStatusEffects();
            
            GD.Print("All GameState tests passed!");
        }

        private static void TestCreateInitial()
        {
            var gameState = GameState.CreateInitial();

            Assert(gameState != null, "GameState should not be null");
            Assert(gameState.Cards != null, "Cards should not be null");
            Assert(gameState.Hand != null, "Hand should not be null");
            Assert(gameState.Player != null, "Player should not be null");
            Assert(gameState.Phase != null, "Phase should not be null");
            Assert(gameState.Spell != null, "Spell should not be null");
            Assert(gameState.StatusEffects != null, "StatusEffects should not be null");
            Assert(gameState.StateId != Guid.Empty, "StateId should not be empty");
            Assert(gameState.CreatedAt <= DateTime.UtcNow, "CreatedAt should be valid");
            Assert(gameState.IsValid(), "GameState should be valid");
        }

        private static void TestCreateInitialSpellState()
        {
            var gameState = GameState.CreateInitial();

            Assert(gameState.Spell.IsActive == false, "Spell should not be active initially");
            Assert(gameState.Spell.Properties.Count == 0, "Spell properties should be empty initially");
            Assert(gameState.Spell.ActiveModifiers.Length == 0, "Spell modifiers should be empty initially");
            Assert(gameState.Spell.TotalDamageDealt == 0f, "Spell damage should be zero initially");
            Assert(gameState.Spell.History.Length == 0, "Spell history should be empty initially");
            Assert(gameState.Spell.StartTime == null, "Spell start time should be null initially");
            Assert(gameState.Spell.CurrentActionIndex == 0, "Spell action index should be zero initially");
        }

        private static void TestCreateInitialStatusEffectsState()
        {
            var gameState = GameState.CreateInitial();

            Assert(gameState.StatusEffects.ActiveEffects.Length == 0, "Status effects should be empty initially");
            Assert(gameState.StatusEffects.TotalActiveEffects == 0, "Total active effects should be zero initially");
            Assert(gameState.StatusEffects.HasAnyActiveEffects == false, "Should have no active effects initially");
        }

        private static void TestCreateWithAllParameters()
        {
            var testCardsState = new CardsState();
            var testHandState = new HandState();
            var testPlayerState = new PlayerState();
            var testPhaseState = new GamePhaseState();
            var testSpellState = SpellState.CreateInitial();
            var testStatusEffectsState = StatusEffectsState.CreateInitial();

            var gameState = GameState.Create(
                testCardsState,
                testHandState,
                testPlayerState,
                testPhaseState,
                testSpellState,
                testStatusEffectsState);

            Assert(gameState.Cards.Equals(testCardsState), "Cards should match provided value");
            Assert(gameState.Hand.Equals(testHandState), "Hand should match provided value");
            Assert(gameState.Player.Equals(testPlayerState), "Player should match provided value");
            Assert(gameState.Phase.Equals(testPhaseState), "Phase should match provided value");
            Assert(gameState.Spell.Equals(testSpellState), "Spell should match provided value");
            Assert(gameState.StatusEffects.Equals(testStatusEffectsState), "StatusEffects should match provided value");
        }

        private static void TestWithSpell()
        {
            var originalState = GameState.CreateInitial();
            var newSpellState = SpellState.CreateInitial().WithActiveSpell(DateTime.UtcNow);

            var updatedState = originalState.WithSpell(newSpellState);

            Assert(!ReferenceEquals(updatedState, originalState), "Updated state should be a new instance");
            Assert(updatedState.Spell.Equals(newSpellState), "Spell should be updated");
            Assert(updatedState.Cards.Equals(originalState.Cards), "Cards should remain unchanged");
            Assert(updatedState.Hand.Equals(originalState.Hand), "Hand should remain unchanged");
            Assert(updatedState.Player.Equals(originalState.Player), "Player should remain unchanged");
            Assert(updatedState.Phase.Equals(originalState.Phase), "Phase should remain unchanged");
            Assert(updatedState.StatusEffects.Equals(originalState.StatusEffects), "StatusEffects should remain unchanged");
        }

        private static void TestWithSpellNull()
        {
            var gameState = GameState.CreateInitial();

            try
            {
                gameState.WithSpell(null);
                Assert(false, "Should throw ArgumentNullException for null spell state");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithStatusEffects()
        {
            var originalState = GameState.CreateInitial();
            var newStatusEffectsState = new StatusEffectsState();

            var updatedState = originalState.WithStatusEffects(newStatusEffectsState);

            Assert(!ReferenceEquals(updatedState, originalState), "Updated state should be a new instance");
            Assert(updatedState.StatusEffects.Equals(newStatusEffectsState), "StatusEffects should be updated");
            Assert(updatedState.Cards.Equals(originalState.Cards), "Cards should remain unchanged");
            Assert(updatedState.Hand.Equals(originalState.Hand), "Hand should remain unchanged");
            Assert(updatedState.Player.Equals(originalState.Player), "Player should remain unchanged");
            Assert(updatedState.Phase.Equals(originalState.Phase), "Phase should remain unchanged");
            Assert(updatedState.Spell.Equals(originalState.Spell), "Spell should remain unchanged");
        }

        private static void TestWithStatusEffectsNull()
        {
            var gameState = GameState.CreateInitial();

            try
            {
                gameState.WithStatusEffects(null);
                Assert(false, "Should throw ArgumentNullException for null status effects state");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithComponents()
        {
            var originalState = GameState.CreateInitial();
            var newSpellState = SpellState.CreateInitial().WithActiveSpell(DateTime.UtcNow);
            var newStatusEffectsState = new StatusEffectsState();

            var updatedState = originalState.WithComponents(
                newSpell: newSpellState,
                newStatusEffects: newStatusEffectsState);

            Assert(updatedState.Spell.Equals(newSpellState), "Spell should be updated");
            Assert(updatedState.StatusEffects.Equals(newStatusEffectsState), "StatusEffects should be updated");
            Assert(updatedState.Cards.Equals(originalState.Cards), "Cards should remain unchanged");
            Assert(updatedState.Hand.Equals(originalState.Hand), "Hand should remain unchanged");
            Assert(updatedState.Player.Equals(originalState.Player), "Player should remain unchanged");
            Assert(updatedState.Phase.Equals(originalState.Phase), "Phase should remain unchanged");
        }

        private static void TestIsValidWithValidStates()
        {
            var gameState = GameState.CreateInitial();
            Assert(gameState.IsValid(), "GameState with valid states should be valid");
        }

        private static void TestIsValidWithInvalidSpellState()
        {
            var invalidSpellState = new SpellState(
                isActive: true,
                startTime: null); // Invalid: active spell without start time
            var gameState = GameState.CreateInitial().WithSpell(invalidSpellState);

            Assert(!gameState.IsValid(), "GameState with invalid spell state should be invalid");
        }

        private static void TestGetStateSummary()
        {
            var activeSpellState = SpellState.CreateInitial().WithActiveSpell(DateTime.UtcNow);
            var gameState = (GameState)GameState.CreateInitial().WithSpell(activeSpellState);

            var summary = gameState.GetStateSummary();

            Assert(summary.Contains("SpellActive: True"), "Summary should contain spell active status");
            Assert(summary.Contains("StatusEffects: 0"), "Summary should contain status effects count");
        }

        private static void TestDeepCopy()
        {
            var originalState = GameState.CreateInitial();

            var copiedState = originalState.DeepCopy();

            Assert(!ReferenceEquals(copiedState, originalState), "Deep copy should be a new instance");
            Assert(copiedState.Equals(originalState), "Deep copy should be equal to original");
            Assert(copiedState.StateId == originalState.StateId, "Deep copy should have same StateId");
            Assert(copiedState.CreatedAt == originalState.CreatedAt, "Deep copy should have same CreatedAt");
        }

        private static void TestEquality()
        {
            var state1 = GameState.CreateInitial();
            var state2 = GameState.Create(
                state1.Cards,
                state1.Hand,
                state1.Player,
                state1.Phase,
                state1.Spell,
                state1.StatusEffects);

            Assert(state1.Equals(state2), "Identical states should be equal");
            Assert(state1.GetHashCode() == state2.GetHashCode(), "Identical states should have same hash code");
        }

        private static void TestEqualityWithDifferentSpellStates()
        {
            var state1 = GameState.CreateInitial();
            var state2 = state1.WithSpell(SpellState.CreateInitial().WithActiveSpell(DateTime.UtcNow));

            Assert(!state1.Equals(state2), "States with different spell states should not be equal");
        }

        private static void TestEqualityWithDifferentStatusEffectsStates()
        {
            var state1 = GameState.CreateInitial();
            var statusEffectInstance = new StatusEffectInstanceData(
                StatusEffectType.Burning,
                5,
                null,
                DateTime.UtcNow);
            var state2 = state1.WithStatusEffects(
                state1.StatusEffects.WithAddedEffect(statusEffectInstance));

            Assert(!state1.Equals(state2), "States with different status effects should not be equal");
        }

        private static void TestToString()
        {
            var gameState = GameState.CreateInitial();

            var stringRepresentation = gameState.ToString();

            Assert(stringRepresentation == gameState.GetStateSummary(), "ToString should return state summary");
        }

        private static void TestConstructorWithNullSpellState()
        {
            var testCardsState = new CardsState();
            var testHandState = new HandState();
            var testPlayerState = new PlayerState();
            var testPhaseState = new GamePhaseState();
            var testStatusEffectsState = StatusEffectsState.CreateInitial();

            try
            {
                GameState.Create(testCardsState, testHandState, testPlayerState, testPhaseState, null, testStatusEffectsState);
                Assert(false, "Should throw ArgumentNullException for null spell state");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestConstructorWithNullStatusEffectsState()
        {
            var testCardsState = new CardsState();
            var testHandState = new HandState();
            var testPlayerState = new PlayerState();
            var testPhaseState = new GamePhaseState();
            var testSpellState = SpellState.CreateInitial();

            try
            {
                GameState.Create(testCardsState, testHandState, testPlayerState, testPhaseState, testSpellState, null);
                Assert(false, "Should throw ArgumentNullException for null status effects state");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithCardsPreservesSpellAndStatusEffects()
        {
            var originalState = GameState.CreateInitial();
            var newCardsState = new CardsState();

            var updatedState = originalState.WithCards(newCardsState);

            Assert(updatedState.Spell.Equals(originalState.Spell), "WithCards should preserve spell state");
            Assert(updatedState.StatusEffects.Equals(originalState.StatusEffects), "WithCards should preserve status effects state");
        }

        private static void TestWithHandPreservesSpellAndStatusEffects()
        {
            var originalState = GameState.CreateInitial();
            var newHandState = new HandState();

            var updatedState = originalState.WithHand(newHandState);

            Assert(updatedState.Spell.Equals(originalState.Spell), "WithHand should preserve spell state");
            Assert(updatedState.StatusEffects.Equals(originalState.StatusEffects), "WithHand should preserve status effects state");
        }

        private static void TestWithPlayerPreservesSpellAndStatusEffects()
        {
            var originalState = GameState.CreateInitial();
            var newPlayerState = new PlayerState();

            var updatedState = originalState.WithPlayer(newPlayerState);

            Assert(updatedState.Spell.Equals(originalState.Spell), "WithPlayer should preserve spell state");
            Assert(updatedState.StatusEffects.Equals(originalState.StatusEffects), "WithPlayer should preserve status effects state");
        }

        private static void TestWithPhasePreservesSpellAndStatusEffects()
        {
            var originalState = GameState.CreateInitial();
            var newPhaseState = new GamePhaseState();

            var updatedState = originalState.WithPhase(newPhaseState);

            Assert(updatedState.Spell.Equals(originalState.Spell), "WithPhase should preserve spell state");
            Assert(updatedState.StatusEffects.Equals(originalState.StatusEffects), "WithPhase should preserve status effects state");
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