using System;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for ExecuteCardActionCommand
    /// </summary>
    public partial class ExecuteCardActionCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Spell Commands Tests] Running ExecuteCardActionCommand Tests...");
            
            TestConstructorWithNullAction();
            TestConstructorWithNullCardId();
            TestGetDescription();
            TestCommandCreation();
            
            GD.Print("[Spell Commands Tests] ExecuteCardActionCommand Tests completed successfully!");
        }

        private static void TestConstructorWithNullAction()
        {
            GD.Print("  Testing Constructor with null action...");
            
            try
            {
                var command = new ExecuteCardActionCommand(null, "card1");
                throw new Exception("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // Expected behavior
                GD.Print("    ✓ Constructor with null action test passed");
            }
        }

        private static void TestConstructorWithNullCardId()
        {
            GD.Print("  Testing Constructor with null cardId...");
            
            // Create a simple damage action for testing
            var damageAction = new DamageActionResource();
            
            try
            {
                var command = new ExecuteCardActionCommand(damageAction, null);
                throw new Exception("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // Expected behavior
                GD.Print("    ✓ Constructor with null cardId test passed");
            }
        }

        private static void TestGetDescription()
        {
            GD.Print("  Testing GetDescription...");
            
            var damageAction = new DamageActionResource();
            var command = new ExecuteCardActionCommand(damageAction, "card1");
            var description = command.GetDescription();
            
            if (!description.Contains("DamageActionResource") || !description.Contains("card1"))
            {
                throw new Exception($"Description should contain action type and card ID, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription test passed");
        }

        private static void TestCommandCreation()
        {
            GD.Print("  Testing Command creation...");
            
            var damageAction = new DamageActionResource();
            var command = new ExecuteCardActionCommand(damageAction, "testCard");
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            if (command.IsBlocking != false)
            {
                throw new Exception("ExecuteCardActionCommand should not be blocking");
            }
            
            GD.Print("    ✓ Command creation test passed");
        }
    }
}