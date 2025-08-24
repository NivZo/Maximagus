using Godot;
using Maximagus.Resources.Definitions.Actions;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Tests
{
    /// <summary>
    /// Simple console test for ActionResource implementations to verify they work correctly
    /// </summary>
    public partial class ActionResourceConsoleTest : RefCounted
    {
        public static void RunConsoleTest()
        {
            GD.Print("=== ActionResource Console Test ===");
            
            try
            {
                TestDamageActionResourceCreation();
                TestModifierActionResourceCreation();
                TestStatusEffectActionResourceCreation();
                
                GD.Print("=== ActionResource Console Test Passed! ===");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"ActionResource Console Test failed: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void TestDamageActionResourceCreation()
        {
            GD.Print("Testing DamageActionResource command creation...");
            
            var damageAction = new DamageActionResource
            {
                DamageType = DamageType.Fire,
                Amount = 10
            };
            
            var command = damageAction.CreateExecutionCommand("test-card-123");
            
            if (command == null)
            {
                throw new System.Exception("DamageActionResource should create a command");
            }
            
            if (!(command is ExecuteCardActionCommand))
            {
                throw new System.Exception("DamageActionResource should create ExecuteCardActionCommand");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ DamageActionResource created command successfully: {description}");
        }

        private static void TestModifierActionResourceCreation()
        {
            GD.Print("Testing ModifierActionResource command creation...");
            
            var modifierAction = new ModifierActionResource
            {
                ModifierType = ModifierType.Add,
                Element = DamageType.Fire,
                Value = 5.0f,
                IsConsumedOnUse = true
            };
            
            var command = modifierAction.CreateExecutionCommand("test-card-456");
            
            if (command == null)
            {
                throw new System.Exception("ModifierActionResource should create a command");
            }
            
            if (!(command is AddSpellModifierCommand))
            {
                throw new System.Exception("ModifierActionResource should create AddSpellModifierCommand");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ ModifierActionResource created command successfully: {description}");
        }

        private static void TestStatusEffectActionResourceCreation()
        {
            GD.Print("Testing StatusEffectActionResource command creation...");
            
            var statusEffect = new StatusEffectResource
            {
                EffectType = StatusEffectType.Poison,
                EffectName = "Poison"
            };
            
            var statusEffectAction = new StatusEffectActionResource
            {
                StatusEffect = statusEffect,
                ActionType = StatusEffectActionType.Add,
                Stacks = 2
            };
            
            var command = statusEffectAction.CreateExecutionCommand("test-card-789");
            
            if (command == null)
            {
                throw new System.Exception("StatusEffectActionResource should create a command");
            }
            
            if (!(command is ApplyStatusEffectCommand))
            {
                throw new System.Exception("StatusEffectActionResource should create ApplyStatusEffectCommand");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ StatusEffectActionResource created command successfully: {description}");
        }
    }
}