using Godot;
using Scripts.Commands.Spell;
using Scripts.State;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;

namespace Tests
{
    /// <summary>
    /// Simple console test for status effect commands to verify they work correctly
    /// </summary>
    public partial class StatusEffectCommandsConsoleTest : RefCounted
    {
        public static void RunConsoleTest()
        {
            GD.Print("=== Status Effect Commands Console Test ===");
            
            try
            {
                TestApplyStatusEffectCommandCreation();
                TestTriggerStatusEffectsCommandCreation();
                TestProcessStatusEffectDecayCommandCreation();
                
                GD.Print("=== Status Effect Commands Console Test Passed! ===");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Status Effect Commands Console Test failed: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void TestApplyStatusEffectCommandCreation()
        {
            GD.Print("Testing ApplyStatusEffectCommand creation...");
            
            var statusEffect = new StatusEffectResource();
            statusEffect.EffectType = StatusEffectType.Poison;
            
            var command = new ApplyStatusEffectCommand(statusEffect, 2, StatusEffectActionType.Add);
            
            if (command == null)
            {
                throw new System.Exception("ApplyStatusEffectCommand should not be null");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ ApplyStatusEffectCommand created successfully: {description}");
        }

        private static void TestTriggerStatusEffectsCommandCreation()
        {
            GD.Print("Testing TriggerStatusEffectsCommand creation...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.StartOfTurn);
            
            if (command == null)
            {
                throw new System.Exception("TriggerStatusEffectsCommand should not be null");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ TriggerStatusEffectsCommand created successfully: {description}");
        }

        private static void TestProcessStatusEffectDecayCommandCreation()
        {
            GD.Print("Testing ProcessStatusEffectDecayCommand creation...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.EndOfTurn);
            
            if (command == null)
            {
                throw new System.Exception("ProcessStatusEffectDecayCommand should not be null");
            }
            
            var description = command.GetDescription();
            if (string.IsNullOrEmpty(description))
            {
                throw new System.Exception("Command description should not be empty");
            }
            
            GD.Print($"  ✓ ProcessStatusEffectDecayCommand created successfully: {description}");
        }
    }
}