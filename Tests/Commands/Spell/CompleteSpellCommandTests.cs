using System;
using System.Collections.Generic;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Spells.Abstractions;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for CompleteSpellCommand
    /// </summary>
    public partial class CompleteSpellCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Spell Commands Tests] Running CompleteSpellCommand Tests...");
            
            TestCommandCreation();
            TestGetDescriptionWithCards();
            TestGetDescriptionWithoutCards();
            TestCommandProperties();
            
            GD.Print("[Spell Commands Tests] CompleteSpellCommand Tests completed successfully!");
        }

        private static void TestCommandCreation()
        {
            GD.Print("  Testing Command creation...");
            
            var command = new CompleteSpellCommand();
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            if (command.IsBlocking != false)
            {
                throw new Exception("CompleteSpellCommand should not be blocking");
            }
            
            GD.Print("    ✓ Command creation test passed");
        }

        private static void TestGetDescriptionWithCards()
        {
            GD.Print("  Testing GetDescription with cards...");
            
            // Create test cards
            var card1 = new SpellCardResource();
            card1.CardName = "Fire Bolt";
            var card2 = new SpellCardResource();
            card2.CardName = "Frost Bolt";
            
            var castCards = new List<SpellCardResource> { card1, card2 };
            var command = new CompleteSpellCommand(castCards, true);
            var description = command.GetDescription();
            
            if (!description.Contains("Fire Bolt") || !description.Contains("Frost Bolt") || !description.Contains("Success: True"))
            {
                throw new Exception($"Description should contain card names and success status, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription with cards test passed");
        }

        private static void TestGetDescriptionWithoutCards()
        {
            GD.Print("  Testing GetDescription without cards...");
            
            var command = new CompleteSpellCommand();
            var description = command.GetDescription();
            
            if (!description.Contains("no cards") || !description.Contains("Success: True"))
            {
                throw new Exception($"Description should indicate no cards and success status, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription without cards test passed");
        }

        private static void TestCommandProperties()
        {
            GD.Print("  Testing Command properties...");
            
            // Test with failed spell
            var command = new CompleteSpellCommand(null, false, "Test error");
            var description = command.GetDescription();
            
            if (!description.Contains("Success: False"))
            {
                throw new Exception($"Failed spell should show Success: False, got: {description}");
            }
            
            GD.Print("    ✓ Command properties test passed");
        }
    }
}