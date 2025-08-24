using System;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Enums;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for AddSpellModifierCommand
    /// </summary>
    public partial class AddSpellModifierCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Spell Commands Tests] Running AddSpellModifierCommand Tests...");
            
            TestConstructorWithValidParameters();
            TestConstructorWithConditions();
            TestGetDescription();
            TestCommandProperties();
            
            GD.Print("[Spell Commands Tests] AddSpellModifierCommand Tests completed successfully!");
        }

        private static void TestConstructorWithValidParameters()
        {
            GD.Print("  Testing Constructor with valid parameters...");
            
            var command = new AddSpellModifierCommand(
                ModifierType.Add, 
                DamageType.Fire, 
                5f, 
                true);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            GD.Print("    ✓ Constructor with valid parameters test passed");
        }

        private static void TestConstructorWithConditions()
        {
            GD.Print("  Testing Constructor with conditions...");
            
            var conditions = new[] { SpellModifierCondition.IsFire };
            var command = new AddSpellModifierCommand(
                ModifierType.Multiply, 
                DamageType.Frost, 
                1.5f, 
                false, 
                conditions);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            GD.Print("    ✓ Constructor with conditions test passed");
        }

        private static void TestGetDescription()
        {
            GD.Print("  Testing GetDescription...");
            
            var command = new AddSpellModifierCommand(ModifierType.Multiply, DamageType.Frost, 1.5f);
            var description = command.GetDescription();
            
            if (!description.Contains("Multiply") || !description.Contains("1.5") || !description.Contains("Frost"))
            {
                throw new Exception($"Description should contain modifier type, value, and element, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription test passed");
        }

        private static void TestCommandProperties()
        {
            GD.Print("  Testing Command properties...");
            
            var command = new AddSpellModifierCommand(ModifierType.Add, DamageType.Fire, 5f);
            
            if (command.IsBlocking != false)
            {
                throw new Exception("AddSpellModifierCommand should not be blocking");
            }
            
            // Test with null conditions (should use empty array)
            var commandWithNullConditions = new AddSpellModifierCommand(
                ModifierType.Add, 
                DamageType.Fire, 
                3f, 
                true, 
                null);
            
            if (commandWithNullConditions == null)
            {
                throw new Exception("Command with null conditions should not be null");
            }
            
            GD.Print("    ✓ Command properties test passed");
        }
    }
}