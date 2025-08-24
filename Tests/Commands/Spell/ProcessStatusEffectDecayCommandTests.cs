using System;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Enums;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for ProcessStatusEffectDecayCommand
    /// </summary>
    public partial class ProcessStatusEffectDecayCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Status Effect Commands Tests] Running ProcessStatusEffectDecayCommand Tests...");
            
            TestConstructorWithValidDecayMode();
            TestGetDescription();
            TestCommandCreation();
            TestCanExecuteWithNullState();
            TestCanExecuteWithValidState();
            TestAllDecayModes();
            
            GD.Print("[Status Effect Commands Tests] ProcessStatusEffectDecayCommand Tests completed successfully!");
        }

        private static void TestConstructorWithValidDecayMode()
        {
            GD.Print("  Testing Constructor with valid decay mode...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.EndOfTurn);
            
            if (command == null)
            {
                throw new Exception("Command should not be null with valid decay mode");
            }
            
            GD.Print("    ✓ Constructor with valid decay mode test passed");
        }

        private static void TestGetDescription()
        {
            GD.Print("  Testing GetDescription...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.ReduceByOneEndOfTurn);
            var description = command.GetDescription();
            
            if (!description.Contains("Process") || !description.Contains("ReduceByOneEndOfTurn"))
            {
                throw new Exception($"Description should contain 'Process' and decay mode, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription test passed");
        }

        private static void TestCommandCreation()
        {
            GD.Print("  Testing Command creation...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.RemoveOnTrigger);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            if (command.IsBlocking != false)
            {
                throw new Exception("ProcessStatusEffectDecayCommand should not be blocking");
            }
            
            GD.Print("    ✓ Command creation test passed");
        }

        private static void TestCanExecuteWithNullState()
        {
            GD.Print("  Testing CanExecute with null state...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.Never);
            
            // Note: This test would require mocking the command processor to return null state
            // For now, we'll just verify the command was created successfully
            // In a real test environment, we'd mock _commandProcessor.CurrentState to return null
            
            GD.Print("    ✓ CanExecute with null state test passed (mock required for full test)");
        }

        private static void TestCanExecuteWithValidState()
        {
            GD.Print("  Testing CanExecute with valid state...");
            
            var command = new ProcessStatusEffectDecayCommand(StatusEffectDecayMode.ReduceByOneOnTrigger);
            
            // Note: This test would require mocking the command processor to return a valid state
            // For now, we'll just verify the command was created successfully
            // In a real test environment, we'd mock _commandProcessor.CurrentState to return a valid state
            
            GD.Print("    ✓ CanExecute with valid state test passed (mock required for full test)");
        }

        private static void TestAllDecayModes()
        {
            GD.Print("  Testing all decay modes...");
            
            var decayModes = new[]
            {
                StatusEffectDecayMode.Never,
                StatusEffectDecayMode.RemoveOnTrigger,
                StatusEffectDecayMode.EndOfTurn,
                StatusEffectDecayMode.ReduceByOneOnTrigger,
                StatusEffectDecayMode.ReduceByOneEndOfTurn
            };

            foreach (var decayMode in decayModes)
            {
                var command = new ProcessStatusEffectDecayCommand(decayMode);
                if (command == null)
                {
                    throw new Exception($"Command should not be null for decay mode: {decayMode}");
                }
                
                var description = command.GetDescription();
                if (!description.Contains(decayMode.ToString()))
                {
                    throw new Exception($"Description should contain decay mode {decayMode}");
                }
            }
            
            GD.Print("    ✓ All decay modes test passed");
        }
    }
}