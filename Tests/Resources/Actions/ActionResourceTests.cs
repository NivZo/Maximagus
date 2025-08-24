using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Tests.Resources.Actions
{
    /// <summary>
    /// Unit tests for ActionResource implementations
    /// </summary>
    public static class ActionResourceTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running ActionResource tests...");
            
            TestDamageActionResource_GetPopUpEffectText();
            TestDamageActionResource_PopUpEffectColor();
            TestDamageActionResource_CreateExecutionCommand();
            TestModifierActionResource_GetPopUpEffectText();
            TestModifierActionResource_CreateExecutionCommand();
            TestStatusEffectActionResource_GetPopUpEffectText();
            TestStatusEffectActionResource_CreateExecutionCommand();
            TestModifierActionResource_CanApply();
            TestModifierActionResource_Apply();
            
            GD.Print("All ActionResource tests passed!");
        }

        private static void TestDamageActionResource_GetPopUpEffectText()
        {
            var gameState = CreateMockGameState();
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 10
            };

            var result = damageAction.GetPopUpEffectText(gameState);

            Assert(result.StartsWith("-"), "Damage popup should start with '-'");
            Assert(result.Contains("15"), "Should show modified damage (10 base + 5 modifier)");
        }

        private static void TestDamageActionResource_PopUpEffectColor()
        {
            var fireAction = new DamageActionResource { DamageType = DamageType.Fire };
            var frostAction = new DamageActionResource { DamageType = DamageType.Frost };

            Assert(fireAction.PopUpEffectColor.Equals(new Color(1, 0.5f, 0)), "Fire damage should be orange");
            Assert(frostAction.PopUpEffectColor.Equals(new Color(0, 0.5f, 1)), "Frost damage should be blue");
        }

        private static void TestDamageActionResource_CreateExecutionCommand()
        {
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 10
            };
            var cardId = "test-card-123";

            var command = damageAction.CreateExecutionCommand(cardId);

            Assert(command is ExecuteCardActionCommand, "Should create ExecuteCardActionCommand");
            Assert(command.GetDescription().Contains("ExecuteCardActionCommand"), "Description should contain command type");
            Assert(command.GetDescription().Contains(cardId), "Description should contain card ID");
        }

        private static void TestModifierActionResource_GetPopUpEffectText()
        {
            var gameState = CreateMockGameState();
            var addModifier = new ModifierActionResource
            {
                ModifierType = ModifierType.Add,
                Value = 5.0f
            };
            var multiplyModifier = new ModifierActionResource
            {
                ModifierType = ModifierType.Multiply,
                Value = 2.0f
            };

            Assert(addModifier.GetPopUpEffectText(gameState) == "+5", "Add modifier should show +5");
            Assert(multiplyModifier.GetPopUpEffectText(gameState) == "x2", "Multiply modifier should show x2");
        }

        private static void TestModifierActionResource_CreateExecutionCommand()
        {
            var modifierAction = new ModifierActionResource
            {
                ModifierType = ModifierType.Add,
                Element = DamageType.Fire,
                Value = 5.0f,
                IsConsumedOnUse = true
            };
            var cardId = "test-card-123";

            var command = modifierAction.CreateExecutionCommand(cardId);

            Assert(command is AddSpellModifierCommand, "Should create AddSpellModifierCommand");
            Assert(command.GetDescription().Contains("Add spell modifier"), "Description should contain modifier info");
        }

        private static void TestStatusEffectActionResource_GetPopUpEffectText()
        {
            var gameState = CreateMockGameState();
            var mockStatusEffect = new MockStatusEffectResource { EffectType = StatusEffectType.Poison };
            var addAction = new StatusEffectActionResource
            {
                StatusEffect = mockStatusEffect,
                ActionType = StatusEffectActionType.Add,
                Stacks = 3
            };

            var result = addAction.GetPopUpEffectText(gameState);

            Assert(result == "+3 Poison", "Should show correct status effect format");
        }

        private static void TestStatusEffectActionResource_CreateExecutionCommand()
        {
            var mockStatusEffect = new MockStatusEffectResource { EffectType = StatusEffectType.Poison };
            var statusEffectAction = new StatusEffectActionResource
            {
                StatusEffect = mockStatusEffect,
                ActionType = StatusEffectActionType.Add,
                Stacks = 2
            };
            var cardId = "test-card-123";

            var command = statusEffectAction.CreateExecutionCommand(cardId);

            Assert(command is ApplyStatusEffectCommand, "Should create ApplyStatusEffectCommand");
            Assert(command.GetDescription().Contains("Apply Add 2 stacks of Poison"), "Description should contain effect info");
        }

        private static void TestModifierActionResource_CanApply()
        {
            var fireModifier = new ModifierActionResource
            {
                SpellModifierConditions = new Godot.Collections.Array<SpellModifierCondition> 
                { 
                    SpellModifierCondition.IsFire 
                }
            };
            var fireDamage = new DamageActionResource { DamageType = DamageType.Fire };
            var frostDamage = new DamageActionResource { DamageType = DamageType.Frost };

            Assert(fireModifier.CanApply(fireDamage), "Fire modifier should apply to fire damage");
            Assert(!fireModifier.CanApply(frostDamage), "Fire modifier should not apply to frost damage");
        }

        private static void TestModifierActionResource_Apply()
        {
            var addModifier = new ModifierActionResource
            {
                ModifierType = ModifierType.Add,
                Value = 5.0f
            };
            var multiplyModifier = new ModifierActionResource
            {
                ModifierType = ModifierType.Multiply,
                Value = 2.0f
            };
            var setModifier = new ModifierActionResource
            {
                ModifierType = ModifierType.Set,
                Value = 15.0f
            };

            Assert(Math.Abs(addModifier.Apply(10.0f) - 15.0f) < 0.001f, "Add modifier should add value");
            Assert(Math.Abs(multiplyModifier.Apply(10.0f) - 20.0f) < 0.001f, "Multiply modifier should multiply value");
            Assert(Math.Abs(setModifier.Apply(10.0f) - 15.0f) < 0.001f, "Set modifier should set value");
        }

        private static IGameStateData CreateMockGameState()
        {
            var modifiers = ImmutableArray.Create(
                new ModifierData(ModifierType.Add, DamageType.Fire, 5.0f, true, ImmutableArray<SpellModifierCondition>.Empty)
            );

            var spellState = new SpellState(
                isActive: true,
                properties: ImmutableDictionary<string, Variant>.Empty,
                activeModifiers: modifiers,
                totalDamageDealt: 0.0f,
                history: ImmutableArray<SpellHistoryEntry>.Empty,
                startTime: System.DateTime.Now,
                currentActionIndex: 0
            );

            var statusEffectsState = new StatusEffectsState(ImmutableArray<StatusEffectInstanceData>.Empty);

            return new TestGameState(spellState, statusEffectsState);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }

    // Mock classes for testing
    public partial class MockStatusEffectResource : StatusEffectResource
    {
        public MockStatusEffectResource()
        {
            EffectName = EffectType.ToString();
        }
    }

    public class TestGameState : IGameStateData
    {
        public SpellState Spell { get; }
        public StatusEffectsState StatusEffects { get; }
        
        // Required properties (not used in these tests)
        public CardsState Cards => null;
        public HandState Hand => null;
        public PlayerState Player => null;
        public GamePhaseState Phase => null;

        public TestGameState(SpellState spellState, StatusEffectsState statusEffectsState)
        {
            Spell = spellState;
            StatusEffects = statusEffectsState;
        }

        // Required methods (not used in these tests)
        public IGameStateData WithCards(CardsState newCardsState) => this;
        public IGameStateData WithHand(HandState newHandState) => this;
        public IGameStateData WithPlayer(PlayerState newPlayerState) => this;
        public IGameStateData WithPhase(GamePhaseState newPhaseState) => this;
        public IGameStateData WithSpell(SpellState newSpellState) => new TestGameState(newSpellState, StatusEffects);
        public IGameStateData WithStatusEffects(StatusEffectsState newStatusEffectsState) => new TestGameState(Spell, newStatusEffectsState);
        public bool IsValid() => true;
        public override string ToString() => "TestGameState";
    }
}