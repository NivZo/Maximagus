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
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        public static void RunConsoleTest()
        {
            _logger.LogInfo("=== ActionResource Console Test ===");
            
            try
            {
                TestDamageActionResourceCreation();
                TestModifierActionResourceCreation();
                TestStatusEffectActionResourceCreation();
                
                _logger.LogInfo("=== ActionResource Console Test Passed! ===");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"ActionResource Console Test failed: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void TestDamageActionResourceCreation()
        {
            _logger.LogInfo("Testing DamageActionResource command creation...");
            
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
            
            _logger.LogInfo($"  ✓ DamageActionResource created command successfully: {description}");
        }

        private static void TestModifierActionResourceCreation()
        {
            _logger.LogInfo("Testing ModifierActionResource command creation...");
            
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
            
            _logger.LogInfo($"  ✓ ModifierActionResource created command successfully: {description}");
        }

        private static void TestStatusEffectActionResourceCreation()
        {
            _logger.LogInfo("Testing StatusEffectActionResource command creation...");
            
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
            
            _logger.LogInfo($"  ✓ StatusEffectActionResource created command successfully: {description}");
        }
    }
}