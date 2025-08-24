using System;
using Scripts.Commands.Spell;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Enums;
using Godot;

namespace Tests.Commands.Spell
{
    /// <summary>
    /// Unit tests for UpdateSpellPropertyCommand
    /// </summary>
    public partial class UpdateSpellPropertyCommandTests : RefCounted
    {
        public static void RunAllTests()
        {
            GD.Print("[Spell Commands Tests] Running UpdateSpellPropertyCommand Tests...");
            
            TestConstructorWithStringKey();
            TestConstructorWithEnumKey();
            TestConstructorWithNullKey();
            TestGetDescriptionWithStringKey();
            TestGetDescriptionWithEnumKey();
            
            GD.Print("[Spell Commands Tests] UpdateSpellPropertyCommand Tests completed successfully!");
        }

        private static void TestConstructorWithStringKey()
        {
            GD.Print("  Testing Constructor with string key...");
            
            var command = new UpdateSpellPropertyCommand("TestKey", Variant.From(5f), ContextPropertyOperation.Add);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            GD.Print("    ✓ Constructor with string key test passed");
        }

        private static void TestConstructorWithEnumKey()
        {
            GD.Print("  Testing Constructor with enum key...");
            
            var command = new UpdateSpellPropertyCommand(ContextProperty.FireDamageDealt, 5f, ContextPropertyOperation.Add);
            
            if (command == null)
            {
                throw new Exception("Command should not be null");
            }
            
            GD.Print("    ✓ Constructor with enum key test passed");
        }

        private static void TestConstructorWithNullKey()
        {
            GD.Print("  Testing Constructor with null key...");
            
            try
            {
                var command = new UpdateSpellPropertyCommand(null, Variant.From(5f), ContextPropertyOperation.Add);
                throw new Exception("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // Expected behavior
                GD.Print("    ✓ Constructor with null key test passed");
            }
        }

        private static void TestGetDescriptionWithStringKey()
        {
            GD.Print("  Testing GetDescription with string key...");
            
            var command = new UpdateSpellPropertyCommand("TestKey", Variant.From(5f), ContextPropertyOperation.Add);
            var description = command.GetDescription();
            
            if (!description.Contains("TestKey") || !description.Contains("Add") || !description.Contains("5"))
            {
                throw new Exception($"Description should contain key, operation, and value, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription with string key test passed");
        }

        private static void TestGetDescriptionWithEnumKey()
        {
            GD.Print("  Testing GetDescription with enum key...");
            
            var command = new UpdateSpellPropertyCommand(ContextProperty.FireDamageDealt, 10f, ContextPropertyOperation.Multiply);
            var description = command.GetDescription();
            
            if (!description.Contains("FireDamageDealt") || !description.Contains("Multiply") || !description.Contains("10"))
            {
                throw new Exception($"Description should contain enum key, operation, and value, got: {description}");
            }
            
            GD.Print("    ✓ GetDescription with enum key test passed");
        }
    }
}