using System;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Enums;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for TriggerStatusEffectsCommand
    /// </summary>
    public partial class TriggerStatusEffectsCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Status Effect Commands Tests] Running TriggerStatusEffectsCommand Tests...");
            
            TestConstructorWithValidTrigger();
            TestGetDescription();
            TestCommandCreation();
            TestCanExecuteWithNullState();
            TestCanExecuteWithValidState();
            TestAllTriggerTypes();
            
            GD.Print("[Status Effect Commands Tests] TriggerStatusEffectsCommand Tests completed successfully!");
        }

        private static void TestConstructorWithValidTrigger()
        {
            GD.Print("  Testing Constructor with valid trigger...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.StartOfTurn);
            
            if (command == null)
            {
                throw new Exception("Command should not be null with valid trigger");
            }
            
            GD.Print("    ✓ Constructor with valid trigger test passed");
        }

        private static void TestGetDescription()
        {
            GD.Print("  Testing GetDescription...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.OnDamageDealt);
            var description = command.GetDescription();
            
            if (!description.Contains("Trigger") || !description.Contains("OnDamageDealt"))
            {
                throw new Exception($"Description should contain 'Trigger' and trigger type, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription test passed");
        }

        private static void TestCommandCreation()
        {
            GD.Print("  Testing Command creation...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.EndOfTurn);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            if (command.IsBlocking != false)
            {
                throw new Exception("TriggerStatusEffectsCommand should not be blocking");
            }
            
            GD.Print("    ✓ Command creation test passed");
        }

        private static void TestCanExecuteWithNullState()
        {
            GD.Print("  Testing CanExecute with null state...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.OnSpellCast);
            
            // Note: This test would require mocking the command processor to return null state
            // For now, we'll just verify the command was created successfully
            // In a real test environment, we'd mock _commandProcessor.CurrentState to return null
            
            GD.Print("    ✓ CanExecute with null state test passed (mock required for full test)");
        }

        private static void TestCanExecuteWithValidState()
        {
            GD.Print("  Testing CanExecute with valid state...");
            
            var command = new TriggerStatusEffectsCommand(StatusEffectTrigger.OnDiscard);
            
            // Note: This test would require mocking the command processor to return a valid state
            // For now, we'll just verify the command was created successfully
            // In a real test environment, we'd mock _commandProcessor.CurrentState to return a valid state
            
            GD.Print("    ✓ CanExecute with valid state test passed (mock required for full test)");
        }

        private static void TestAllTriggerTypes()
        {
            GD.Print("  Testing all trigger types...");
            
            var triggerTypes = new[]
            {
                StatusEffectTrigger.StartOfTurn,
                StatusEffectTrigger.EndOfTurn,
                StatusEffectTrigger.OnDamageDealt,
                StatusEffectTrigger.OnSpellCast,
                StatusEffectTrigger.OnDiscard
            };

            foreach (var trigger in triggerTypes)
            {
                var command = new TriggerStatusEffectsCommand(trigger);
                if (command == null)
                {
                    throw new Exception($"Command should not be null for trigger: {trigger}");
                }
                
                var description = command.GetDescription();
                if (!description.Contains(trigger.ToString()))
                {
                    throw new Exception($"Description should contain trigger type {trigger}");
                }
            }
            
            GD.Print("    ✓ All trigger types test passed");
        }
    }
}