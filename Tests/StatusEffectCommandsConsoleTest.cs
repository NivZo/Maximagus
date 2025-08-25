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
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        public static void RunConsoleTest()
        {
            _logger.LogInfo("=== Status Effect Commands Console Test ===");
            
            try
            {
                TestApplyStatusEffectCommandCreation();
                TestTriggerStatusEffectsCommandCreation();
                TestProcessStatusEffectDecayCommandCreation();
                
                _logger.LogInfo("=== Status Effect Commands Console Test Passed! ===");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Status Effect Commands Console Test failed: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void TestApplyStatusEffectCommandCreation()
        {
            _logger.LogInfo("Testing ApplyStatusEffectCommand creation...");
            
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
            
            _logger.LogInfo($"  ✓ ApplyStatusEffectCommand created successfully: {description}");
        }

        private static void TestTriggerStatusEffectsCommandCreation()
        {
            _logger.LogInfo("Testing TriggerStatusEffectsCommand creation...");
            
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
            
            _logger.LogInfo($"  ✓ TriggerStatusEffectsCommand created successfully: {description}");
        }

        private static void TestProcessStatusEffectDecayCommandCreation()
        {
            _logger.LogInfo("Testing ProcessStatusEffectDecayCommand creation...");
            
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
            
            _logger.LogInfo($"  ✓ ProcessStatusEffectDecayCommand created successfully: {description}");
        }
    }
}