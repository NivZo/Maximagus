using Godot;
using Godot.Collections;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;

namespace Tests.Resources
{
    public static class SpellCardResourceTests
    {
        public static void RunAllTests()
        {
            GD.Print("=== SpellCardResource Tests ===");
            
            TestCreateExecutionCommands_WithNoActions_ReturnsEmptyCollection();
            TestCreateExecutionCommands_WithSingleAction_CallsActionMethod();
            TestCreateExecutionCommands_WithMultipleActions_CallsAllActionMethods();
            TestCreateExecutionCommands_WithNullActions_HandlesGracefully();
            TestCreateExecutionCommands_PassesCardIdToActions();
            TestSpellCardResource_Properties_SetAndGetCorrectly();
            
            GD.Print("=== All SpellCardResource Tests Passed ===");
        }

        private static void TestCreateExecutionCommands_WithNoActions_ReturnsEmptyCollection()
        {
            // Arrange
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card",
                CardName = "Test Card",
                CardDescription = "A test card",
                Actions = new Array<ActionResource>()
            };

            // Act
            var commands = spellCard.CreateExecutionCommands("card123");

            // Assert
            Assert(!commands.Any(), "Should return empty collection when no actions");
            GD.Print("✓ CreateExecutionCommands with no actions returns empty collection");
        }

        private static void TestCreateExecutionCommands_WithSingleAction_CallsActionMethod()
        {
            // Arrange
            var mockAction = new MockActionResource();
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card",
                CardName = "Test Card",
                CardDescription = "A test card",
                Actions = new Array<ActionResource> { mockAction }
            };

            // Act
            var commands = spellCard.CreateExecutionCommands("card123").ToList();

            // Assert
            Assert(commands.Count == 1, "Should return single command for single action");
            Assert(mockAction.CreateExecutionCommandCalled, "Should call CreateExecutionCommand on action");
            GD.Print("✓ CreateExecutionCommands with single action calls action method");
        }

        private static void TestCreateExecutionCommands_WithMultipleActions_CallsAllActionMethods()
        {
            // Arrange
            var mockAction1 = new MockActionResource();
            var mockAction2 = new MockActionResource();
            var mockAction3 = new MockActionResource();
            
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card",
                CardName = "Test Card",
                CardDescription = "A test card",
                Actions = new Array<ActionResource> { mockAction1, mockAction2, mockAction3 }
            };

            // Act
            var commands = spellCard.CreateExecutionCommands("card123").ToList();

            // Assert
            Assert(commands.Count == 3, "Should return three commands for three actions");
            Assert(mockAction1.CreateExecutionCommandCalled, "Should call CreateExecutionCommand on first action");
            Assert(mockAction2.CreateExecutionCommandCalled, "Should call CreateExecutionCommand on second action");
            Assert(mockAction3.CreateExecutionCommandCalled, "Should call CreateExecutionCommand on third action");
            GD.Print("✓ CreateExecutionCommands with multiple actions calls all action methods");
        }

        private static void TestCreateExecutionCommands_WithNullActions_HandlesGracefully()
        {
            // Arrange
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card",
                CardName = "Test Card",
                CardDescription = "A test card",
                Actions = null
            };

            // Act & Assert
            try
            {
                var commands = spellCard.CreateExecutionCommands("card123");
                // If Actions is null, LINQ will throw, which is expected behavior
                Assert(false, "Should handle null Actions gracefully or throw expected exception");
            }
            catch (System.ArgumentNullException)
            {
                // Expected behavior - LINQ Select on null throws ArgumentNullException
                GD.Print("✓ CreateExecutionCommands with null actions throws expected exception");
            }
        }

        private static void TestCreateExecutionCommands_PassesCardIdToActions()
        {
            // Arrange
            var mockAction1 = new MockActionResource { TestId = "action1" };
            var mockAction2 = new MockActionResource { TestId = "action2" };
            var mockAction3 = new MockActionResource { TestId = "action3" };
            
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card",
                CardName = "Test Card",
                CardDescription = "A test card",
                Actions = new Array<ActionResource> { mockAction1, mockAction2, mockAction3 }
            };

            var testCardId = "test-card-id-123";

            // Act
            var commands = spellCard.CreateExecutionCommands(testCardId).ToList();

            // Assert
            Assert(commands.Count == 3, "Should return three commands");
            Assert(mockAction1.LastCardIdPassed == testCardId, "First action should receive correct card ID");
            Assert(mockAction2.LastCardIdPassed == testCardId, "Second action should receive correct card ID");
            Assert(mockAction3.LastCardIdPassed == testCardId, "Third action should receive correct card ID");
            GD.Print("✓ CreateExecutionCommands passes card ID to all actions");
        }

        private static void TestSpellCardResource_Properties_SetAndGetCorrectly()
        {
            // Arrange & Act
            var spellCard = new SpellCardResource
            {
                CardResourceId = "test-card-123",
                CardName = "Fire Bolt",
                CardDescription = "A powerful fire spell that deals damage",
                Actions = new Array<ActionResource>()
            };

            // Assert
            Assert(spellCard.CardResourceId == "test-card-123", "CardResourceId should be set correctly");
            Assert(spellCard.CardName == "Fire Bolt", "CardName should be set correctly");
            Assert(spellCard.CardDescription == "A powerful fire spell that deals damage", "CardDescription should be set correctly");
            Assert(spellCard.Actions != null, "Actions should not be null");
            Assert(spellCard.Actions.Count == 0, "Actions should be empty array");
            GD.Print("✓ SpellCardResource properties set and get correctly");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                GD.PrintErr($"ASSERTION FAILED: {message}");
                throw new System.Exception($"Test assertion failed: {message}");
            }
        }
    }

    // Mock classes for testing - using a real ActionResource implementation to test integration
    public partial class MockActionResource : ActionResource
    {
        public string TestId { get; set; } = "mock";
        public bool CreateExecutionCommandCalled { get; private set; } = false;
        public string LastCardIdPassed { get; private set; } = null;

        public override Color PopUpEffectColor => Colors.White;

        public override string GetPopUpEffectText(Scripts.State.IGameStateData gameState)
        {
            return "Mock Effect";
        }

        public override Scripts.Commands.GameCommand CreateExecutionCommand(string cardId)
        {
            CreateExecutionCommandCalled = true;
            LastCardIdPassed = cardId;
            
            // Return a real command from the existing system to test integration
            return new Scripts.Commands.Spell.ExecuteCardActionCommand(this, cardId);
        }
    }
}