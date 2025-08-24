using System;
using System.Collections.Immutable;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.State;

namespace Tests.Managers
{
    /// <summary>
    /// Comprehensive unit tests for SpellLogicManager
    /// </summary>
    public partial class SpellLogicManagerTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Spell Casting Refactor] Running SpellLogicManager Tests...");
            
            TestPreCalculateActionResult();
            TestApplyDamageModifiers();
            TestAddModifier();
            TestUpdateProperty();
            TestProcessDamageAction();
            TestEncounterStateBasedMethods();
            TestEdgeCases();
            
            GD.Print("[Spell Casting Refactor] SpellLogicManager Tests Completed!");
        }

        private static void TestPreCalculateActionResult()
        {
            GD.Print("Testing PreCalculateActionResult...");

            // Test basic damage calculation without modifiers
            var damageAction = CreateDamageAction(DamageType.Fire, 10);
            var gameState = CreateGameStateWithModifiers(ImmutableArray<ModifierData>.Empty);
            
            var result = SpellLogicManager.PreCalculateActionResult(damageAction, gameState);
            Assert(result.FinalDamage == 10f, $"Expected 10, got {result.FinalDamage}");
            Assert(result.Action.Equals(damageAction), "Action should match");

            // Test with add modifier
            var addModifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, false, 
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithAdd = CreateGameStateWithModifiers(ImmutableArray.Create(addModifier));
            
            result = SpellLogicManager.PreCalculateActionResult(damageAction, gameStateWithAdd);
            Assert(result.FinalDamage == 15f, $"Expected 15, got {result.FinalDamage}");

            // Test with multiply modifier
            var multiplyModifier = new ModifierData(ModifierType.Multiply, DamageType.Fire, 2f, false,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithMultiply = CreateGameStateWithModifiers(ImmutableArray.Create(multiplyModifier));
            
            result = SpellLogicManager.PreCalculateActionResult(damageAction, gameStateWithMultiply);
            Assert(result.FinalDamage == 20f, $"Expected 20, got {result.FinalDamage}");

            // Test with set modifier
            var setModifier = new ModifierData(ModifierType.Set, DamageType.Fire, 25f, false,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithSet = CreateGameStateWithModifiers(ImmutableArray.Create(setModifier));
            
            result = SpellLogicManager.PreCalculateActionResult(damageAction, gameStateWithSet);
            Assert(result.FinalDamage == 25f, $"Expected 25, got {result.FinalDamage}");

            // Test with multiple modifiers
            var gameStateWithMultiple = CreateGameStateWithModifiers(ImmutableArray.Create(addModifier, multiplyModifier));
            result = SpellLogicManager.PreCalculateActionResult(damageAction, gameStateWithMultiple);
            Assert(result.FinalDamage == 30f, $"Expected 30 (10+5)*2, got {result.FinalDamage}");

            // Test with non-matching condition
            var frostDamageAction = CreateDamageAction(DamageType.Frost, 10);
            result = SpellLogicManager.PreCalculateActionResult(frostDamageAction, gameStateWithAdd);
            Assert(result.FinalDamage == 10f, $"Expected 10 (no modifier applied), got {result.FinalDamage}");

            // Test PerChill damage type
            var perChillAction = CreateDamageAction(DamageType.PerChill, 5);
            var gameStateWithChill = CreateGameStateWithStatusEffects(ImmutableArray.Create(
                new StatusEffectInstanceData(StatusEffectType.Chill, 3, null, DateTime.Now)));
            result = SpellLogicManager.PreCalculateActionResult(perChillAction, gameStateWithChill);
            Assert(result.FinalDamage == 15f, $"Expected 15 (5*3 chill stacks), got {result.FinalDamage}");

            // Test consumable modifier tracking
            var consumableModifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithConsumable = CreateGameStateWithModifiers(ImmutableArray.Create(consumableModifier));
            result = SpellLogicManager.PreCalculateActionResult(damageAction, gameStateWithConsumable);
            Assert(result.ConsumedModifiers.Length == 1, $"Expected 1 consumed modifier, got {result.ConsumedModifiers.Length}");
            Assert(result.ConsumedModifiers[0].Equals(consumableModifier), "Wrong modifier marked as consumed");

            GD.Print("PreCalculateActionResult tests passed!");
        }

        private static void TestApplyDamageModifiers()
        {
            GD.Print("Testing ApplyDamageModifiers...");

            var damageAction = CreateDamageAction(DamageType.Fire, 10);

            // Test with consumable modifier
            var consumableModifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithConsumable = CreateGameStateWithModifiers(ImmutableArray.Create(consumableModifier));

            var (finalDamage, remainingModifiers) = SpellLogicManager.ApplyDamageModifiers(damageAction, gameStateWithConsumable);
            Assert(finalDamage == 15f, $"Expected 15, got {finalDamage}");
            Assert(remainingModifiers.Length == 0, $"Expected 0 remaining modifiers, got {remainingModifiers.Length}");

            // Test with non-consumable modifier
            var nonConsumableModifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, false,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithNonConsumable = CreateGameStateWithModifiers(ImmutableArray.Create(nonConsumableModifier));

            (finalDamage, remainingModifiers) = SpellLogicManager.ApplyDamageModifiers(damageAction, gameStateWithNonConsumable);
            Assert(finalDamage == 15f, $"Expected 15, got {finalDamage}");
            Assert(remainingModifiers.Length == 1, $"Expected 1 remaining modifier, got {remainingModifiers.Length}");

            // Test with mixed consumable and non-consumable modifiers
            var gameStateWithMixed = CreateGameStateWithModifiers(ImmutableArray.Create(consumableModifier, nonConsumableModifier));
            (finalDamage, remainingModifiers) = SpellLogicManager.ApplyDamageModifiers(damageAction, gameStateWithMixed);
            Assert(finalDamage == 20f, $"Expected 20 (10+5+5), got {finalDamage}");
            Assert(remainingModifiers.Length == 1, $"Expected 1 remaining modifier, got {remainingModifiers.Length}");
            Assert(remainingModifiers[0].Equals(nonConsumableModifier), "Wrong modifier remained");

            GD.Print("ApplyDamageModifiers tests passed!");
        }

        private static void TestAddModifier()
        {
            GD.Print("Testing AddModifier...");

            var initialState = SpellState.CreateInitial().WithActiveSpell(DateTime.Now);
            var modifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, false);

            var newState = SpellLogicManager.AddModifier(initialState, modifier);
            
            Assert(newState.ActiveModifiers.Length == 1, $"Expected 1 modifier, got {newState.ActiveModifiers.Length}");
            Assert(newState.ActiveModifiers[0].Equals(modifier), "Modifier not added correctly");
            Assert(initialState.ActiveModifiers.Length == 0, "Original state was modified");

            // Test adding multiple modifiers
            var secondModifier = new ModifierData(ModifierType.Multiply, DamageType.Frost, 2f, true);
            var stateWithTwo = SpellLogicManager.AddModifier(newState, secondModifier);
            
            Assert(stateWithTwo.ActiveModifiers.Length == 2, $"Expected 2 modifiers, got {stateWithTwo.ActiveModifiers.Length}");

            GD.Print("AddModifier tests passed!");
        }

        private static void TestUpdateProperty()
        {
            GD.Print("Testing UpdateProperty...");

            var initialState = SpellState.CreateInitial().WithActiveSpell(DateTime.Now);

            // Test Add operation
            var stateWithAdd = SpellLogicManager.UpdateProperty(initialState, "TestProp", Variant.From(10f), ContextPropertyOperation.Add);
            Assert(stateWithAdd.GetProperty("TestProp", 0f) == 10f, "Add operation failed");

            // Test adding to existing property
            var stateWithMoreAdd = SpellLogicManager.UpdateProperty(stateWithAdd, "TestProp", Variant.From(5f), ContextPropertyOperation.Add);
            Assert(stateWithMoreAdd.GetProperty("TestProp", 0f) == 15f, "Add to existing property failed");

            // Test Multiply operation
            var stateWithMultiply = SpellLogicManager.UpdateProperty(stateWithMoreAdd, "TestProp", Variant.From(2f), ContextPropertyOperation.Multiply);
            Assert(stateWithMultiply.GetProperty("TestProp", 0f) == 30f, "Multiply operation failed");

            // Test Set operation
            var stateWithSet = SpellLogicManager.UpdateProperty(stateWithMultiply, "TestProp", Variant.From(100f), ContextPropertyOperation.Set);
            Assert(stateWithSet.GetProperty("TestProp", 0f) == 100f, "Set operation failed");

            // Test ContextProperty enum overload
            var stateWithEnum = SpellLogicManager.UpdateProperty(initialState, ContextProperty.FireDamageDealt, 25f, ContextPropertyOperation.Add);
            Assert(stateWithEnum.GetProperty("FireDamageDealt", 0f) == 25f, "ContextProperty enum overload failed");

            GD.Print("UpdateProperty tests passed!");
        }

        private static void TestProcessDamageAction()
        {
            GD.Print("Testing ProcessDamageAction...");

            var damageAction = CreateDamageAction(DamageType.Fire, 10);

            // Test basic damage processing without modifiers
            var gameState = CreateGameStateWithModifiers(ImmutableArray<ModifierData>.Empty);
            var processedState = SpellLogicManager.ProcessDamageAction(gameState, damageAction);
            
            Assert(processedState.TotalDamageDealt == 10f, $"Expected 10 total damage, got {processedState.TotalDamageDealt}");
            Assert(processedState.GetProperty("FireDamageDealt", 0f) == 10f, "FireDamageDealt property not updated");

            // Test damage processing with modifiers
            var modifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithModifier = CreateGameStateWithModifiers(ImmutableArray.Create(modifier));
            
            var processedWithModifier = SpellLogicManager.ProcessDamageAction(gameStateWithModifier, damageAction);
            
            Assert(processedWithModifier.TotalDamageDealt == 15f, $"Expected 15 total damage, got {processedWithModifier.TotalDamageDealt}");
            Assert(processedWithModifier.GetProperty("FireDamageDealt", 0f) == 15f, "FireDamageDealt property not updated correctly");
            Assert(processedWithModifier.ActiveModifiers.Length == 0, "Consumable modifier not removed");

            // Test frost damage
            var frostAction = CreateDamageAction(DamageType.Frost, 8);
            var processedFrost = SpellLogicManager.ProcessDamageAction(gameState, frostAction);
            
            Assert(processedFrost.GetProperty("FrostDamageDealt", 0f) == 8f, "FrostDamageDealt property not updated");

            // Test PerChill damage
            var perChillAction = CreateDamageAction(DamageType.PerChill, 3);
            var gameStateWithChill = CreateGameStateWithStatusEffects(ImmutableArray.Create(
                new StatusEffectInstanceData(StatusEffectType.Chill, 4, null, DateTime.Now)));
            var processedPerChill = SpellLogicManager.ProcessDamageAction(gameStateWithChill, perChillAction);
            
            Assert(processedPerChill.TotalDamageDealt == 12f, $"Expected 12 total damage (3*4), got {processedPerChill.TotalDamageDealt}");
            Assert(processedPerChill.GetProperty("FrostDamageDealt", 0f) == 12f, "PerChill damage not counted as frost");

            GD.Print("ProcessDamageAction tests passed!");
        }

        private static void TestEncounterStateBasedMethods()
        {
            GD.Print("Testing EncounterState-based methods...");

            // Test PreCalculateActionResult with EncounterState
            var damageAction = CreateDamageAction(DamageType.Fire, 10);
            var gameState = CreateGameStateWithModifiers(ImmutableArray<ModifierData>.Empty);
            var encounterState = EncounterState.FromGameState(gameState, DateTime.UtcNow);
            
            var result = SpellLogicManager.PreCalculateActionResult((ActionResource)damageAction, encounterState);
            Assert(result.FinalDamage == 10f, $"Expected 10, got {result.FinalDamage}");
            Assert(result.Action.Equals(damageAction), "Action should match");

            // Test ApplyDamageModifiers with EncounterState
            var modifier = new ModifierData(ModifierType.Add, DamageType.Fire, 5f, true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            var gameStateWithModifier = CreateGameStateWithModifiers(ImmutableArray.Create(modifier));
            var encounterStateWithModifier = EncounterState.FromGameState(gameStateWithModifier, DateTime.UtcNow);

            var (finalDamage, remainingModifiers) = SpellLogicManager.ApplyDamageModifiers(damageAction, encounterStateWithModifier);
            Assert(finalDamage == 15f, $"Expected 15, got {finalDamage}");
            Assert(remainingModifiers.Length == 0, $"Expected 0 remaining modifiers, got {remainingModifiers.Length}");

            // Test PerChill damage with EncounterState
            var perChillAction = CreateDamageAction(DamageType.PerChill, 3);
            var gameStateWithChill = CreateGameStateWithStatusEffects(ImmutableArray.Create(
                new StatusEffectInstanceData(StatusEffectType.Chill, 4, null, DateTime.Now)));
            var encounterStateWithChill = EncounterState.FromGameState(gameStateWithChill, DateTime.UtcNow);

            result = SpellLogicManager.PreCalculateActionResult(perChillAction, encounterStateWithChill);
            Assert(result.FinalDamage == 12f, $"Expected 12 (3*4 chill stacks), got {result.FinalDamage}");

            // Test PreCalculateActionWithSnapshot
            var snapshot = SpellLogicManager.PreCalculateActionWithSnapshot(damageAction, encounterState);
            Assert(snapshot.ActionKey == damageAction.ActionId, "Action key should match action ID");
            Assert(snapshot.ActionResult.FinalDamage == 10f, $"Expected 10 damage in snapshot, got {snapshot.ActionResult.FinalDamage}");
            Assert(snapshot.ResultingState != null, "Resulting state should not be null");
            Assert(snapshot.IsValid(), "Snapshot should be valid");

            // Test PreCalculateSpellWithSnapshots
            var cardState = CreateCardState(ImmutableArray.Create<ActionResource>(damageAction));
            var snapshots = SpellLogicManager.PreCalculateSpellWithSnapshots(gameState, ImmutableArray.Create(cardState));
            Assert(snapshots.Length == 1, $"Expected 1 snapshot, got {snapshots.Length}");
            Assert(snapshots[0].ActionKey == damageAction.ActionId, "Snapshot action key should match");

            // Test ApplyEncounterSnapshot
            var testSnapshot = EncounterStateSnapshot.Create(
                "test-action",
                encounterState.WithSpell(encounterState.Spell.WithTotalDamage(25f)),
                ActionExecutionResult.CreateForDamage(damageAction, 25f, ImmutableArray<ModifierData>.Empty));

            var updatedGameState = SpellLogicManager.ApplyEncounterSnapshot(gameState, testSnapshot);
            Assert(updatedGameState.Spell.TotalDamageDealt == 25f, $"Expected 25 total damage after applying snapshot, got {updatedGameState.Spell.TotalDamageDealt}");

            GD.Print("EncounterState-based methods tests passed!");
        }

        private static void TestEdgeCases()
        {
            GD.Print("Testing edge cases...");

            // Test null arguments
            try
            {
                SpellLogicManager.PreCalculateActionResult(null, CreateGameStateWithModifiers(ImmutableArray<ModifierData>.Empty));
                Assert(false, "Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                var damageAction = CreateDamageAction(DamageType.Fire, 10);
                SpellLogicManager.PreCalculateActionResult((ActionResource)damageAction, (IGameStateData)null);
                Assert(false, "Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                SpellLogicManager.AddModifier(null, new ModifierData(ModifierType.Add, DamageType.Fire, 1f, false));
                Assert(false, "Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                SpellLogicManager.UpdateProperty(null, "test", Variant.From(1f), ContextPropertyOperation.Add);
                Assert(false, "Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                var state = SpellState.CreateInitial();
                SpellLogicManager.UpdateProperty(state, "", Variant.From(1f), ContextPropertyOperation.Add);
                Assert(false, "Should have thrown ArgumentException for empty key");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            // Test zero damage
            var zeroDamageAction = CreateDamageAction(DamageType.Fire, 0);
            var zeroGameState = CreateGameStateWithModifiers(ImmutableArray<ModifierData>.Empty);
            var processedZero = SpellLogicManager.ProcessDamageAction(zeroGameState, zeroDamageAction);
            
            Assert(processedZero.TotalDamageDealt == 0f, "Zero damage should result in zero total damage");
            Assert(processedZero.GetProperty("FireDamageDealt", 0f) == 0f, "Zero damage should not update damage property");

            // Test unknown damage type (should throw exception)
            try
            {
                var unknownDamageAction = CreateDamageAction((DamageType)999, 10);
                SpellLogicManager.ProcessDamageAction(zeroGameState, unknownDamageAction);
                Assert(false, "Should have thrown ArgumentException for unknown damage type");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            GD.Print("Edge cases tests passed!");
        }

        private static DamageActionResource CreateDamageAction(DamageType damageType, int amount)
        {
            var action = new DamageActionResource();
            action.DamageType = damageType;
            action.Amount = amount;
            return action;
        }

        private static IGameStateData CreateGameStateWithModifiers(ImmutableArray<ModifierData> modifiers)
        {
            var spellState = SpellState.CreateInitial()
                .WithActiveSpell(DateTime.Now)
                .WithModifiers(modifiers);
            
            var statusEffectsState = StatusEffectsState.CreateInitial();
            
            return new TestGameState(spellState, statusEffectsState);
        }

        private static IGameStateData CreateGameStateWithStatusEffects(ImmutableArray<StatusEffectInstanceData> statusEffects)
        {
            var spellState = SpellState.CreateInitial().WithActiveSpell(DateTime.Now);
            var statusEffectsState = new StatusEffectsState(statusEffects);
            
            return new TestGameState(spellState, statusEffectsState);
        }

        private static CardState CreateCardState(ImmutableArray<ActionResource> actions)
        {
            var cardResource = new TestCardResource(actions);
            return new CardState("test-card-id", cardResource);
        }

        // Simple test implementation of card resource for testing
        private partial class TestCardResource : Maximagus.Scripts.Spells.Abstractions.SpellCardResource
        {
            public TestCardResource(ImmutableArray<ActionResource> actions)
            {
                Actions = new Godot.Collections.Array<ActionResource>();
                foreach (var action in actions)
                {
                    Actions.Add(action);
                }
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                GD.PrintErr($"ASSERTION FAILED: {message}");
                throw new Exception($"Test assertion failed: {message}");
            }
        }

        // Simple test implementation of IGameStateData for testing purposes
        private class TestGameState : IGameStateData
        {
            public CardsState Cards { get; }
            public HandState Hand { get; }
            public PlayerState Player { get; }
            public GamePhaseState Phase { get; }
            public SpellState Spell { get; }
            public StatusEffectsState StatusEffects { get; }

            public TestGameState(SpellState spell, StatusEffectsState statusEffects)
            {
                Spell = spell;
                StatusEffects = statusEffects;
                // Initialize other states with defaults for testing
                Cards = null;
                Hand = null;
                Player = null;
                Phase = null;
            }

            public IGameStateData WithCards(CardsState newCardsState) => new TestGameState(Spell, StatusEffects);
            public IGameStateData WithHand(HandState newHandState) => new TestGameState(Spell, StatusEffects);
            public IGameStateData WithPlayer(PlayerState newPlayerState) => new TestGameState(Spell, StatusEffects);
            public IGameStateData WithPhase(GamePhaseState newPhaseState) => new TestGameState(Spell, StatusEffects);
            public IGameStateData WithSpell(SpellState newSpellState) => new TestGameState(newSpellState, StatusEffects);
            public IGameStateData WithStatusEffects(StatusEffectsState newStatusEffectsState) => new TestGameState(Spell, newStatusEffectsState);

            public bool IsValid() => true;
            public override string ToString() => "TestGameState";
        }
    }
}