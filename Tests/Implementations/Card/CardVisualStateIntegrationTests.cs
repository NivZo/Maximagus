using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Scripts.Commands;
using Scripts.State;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;

namespace Tests.Implementations.Card
{
    /// <summary>
    /// Unit tests for Card visual state integration
    /// </summary>
    public static class CardVisualStateIntegrationTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running Card Visual State Integration tests...");
            
            TestOnGameStateChanged_WhenSpellNotActive_ShouldNotShowPopupEffects();
            TestOnGameStateChanged_WhenCardNotInPlayedCards_ShouldNotShowPopupEffects();
            TestOnGameStateChanged_WhenActionIndexAdvances_ShouldShowPopupEffect();
            TestOnGameStateChanged_WhenMultipleActionsOnCard_ShouldShowCorrectAction();
            TestOnGameStateChanged_WhenActionIndexDoesNotAdvance_ShouldNotShowPopupEffect();
            TestOnGameStateChanged_WhenExceptionOccurs_ShouldLogError();
            
            GD.Print("Card Visual State Integration tests completed successfully!");
        }

        private static void TestOnGameStateChanged_WhenSpellNotActive_ShouldNotShowPopupEffects()
        {
            // Arrange
            var (card, _, _) = SetupTest();
            var previousState = CreateGameStateWithInactiveSpell();
            var newState = CreateGameStateWithInactiveSpell();

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertFalse(card.PopupEffectShown, "Popup effect should not be shown when spell is not active");
            GD.Print("✓ OnGameStateChanged_WhenSpellNotActive_ShouldNotShowPopupEffects");
        }

        private static void TestOnGameStateChanged_WhenCardNotInPlayedCards_ShouldNotShowPopupEffects()
        {
            // Arrange
            var (card, _, _) = SetupTest();
            var previousState = CreateGameStateWithActiveSpell(actionIndex: 0);
            var newState = CreateGameStateWithActiveSpell(actionIndex: 1, includeTestCard: false);

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertFalse(card.PopupEffectShown, "Popup effect should not be shown when card is not in played cards");
            GD.Print("✓ OnGameStateChanged_WhenCardNotInPlayedCards_ShouldNotShowPopupEffects");
        }

        private static void TestOnGameStateChanged_WhenActionIndexAdvances_ShouldShowPopupEffect()
        {
            // Arrange
            var (card, _, _) = SetupTest();
            var previousState = CreateGameStateWithActiveSpell(actionIndex: 0);
            var newState = CreateGameStateWithActiveSpell(actionIndex: 1);

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertTrue(card.PopupEffectShown, "Popup effect should be shown when action index advances");
            AssertEqual("Test Popup Text", card.LastPopupText, "Popup text should match action text");
            AssertEqual(Colors.Red, card.LastPopupColor, "Popup color should match action color");
            GD.Print("✓ OnGameStateChanged_WhenActionIndexAdvances_ShouldShowPopupEffect");
        }

        private static void TestOnGameStateChanged_WhenMultipleActionsOnCard_ShouldShowCorrectAction()
        {
            // Arrange
            var (card, _, _) = SetupTest();
            var cardResourceWithMultipleActions = CreateTestCardResourceWithMultipleActions();
            card.SetupForTesting(cardResourceWithMultipleActions, "test-card-1");

            var previousState = CreateGameStateWithActiveSpell(actionIndex: 1, cardResource: cardResourceWithMultipleActions);
            var newState = CreateGameStateWithActiveSpell(actionIndex: 2, cardResource: cardResourceWithMultipleActions);

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertTrue(card.PopupEffectShown, "Popup effect should be shown for second action");
            AssertEqual("Second Action Text", card.LastPopupText, "Should show second action text");
            GD.Print("✓ OnGameStateChanged_WhenMultipleActionsOnCard_ShouldShowCorrectAction");
        }

        private static void TestOnGameStateChanged_WhenActionIndexDoesNotAdvance_ShouldNotShowPopupEffect()
        {
            // Arrange
            var (card, _, _) = SetupTest();
            var previousState = CreateGameStateWithActiveSpell(actionIndex: 1);
            var newState = CreateGameStateWithActiveSpell(actionIndex: 1);

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertFalse(card.PopupEffectShown, "Popup effect should not be shown when action index does not advance");
            GD.Print("✓ OnGameStateChanged_WhenActionIndexDoesNotAdvance_ShouldNotShowPopupEffect");
        }

        private static void TestOnGameStateChanged_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var (card, _, logger) = SetupTest();
            var previousState = CreateGameStateWithActiveSpell(actionIndex: 0);
            var newState = CreateGameStateWithActiveSpell(actionIndex: 1);
            
            // Make the card throw an exception
            card.ShouldThrowException = true;

            // Act
            card.TestOnGameStateChanged(previousState, newState);

            // Assert
            AssertTrue(logger.ErrorLogged, "Error should be logged when exception occurs");
            AssertTrue(logger.LastErrorMessage.Contains("Error handling state change for card"), "Error message should contain expected text");
            GD.Print("✓ OnGameStateChanged_WhenExceptionOccurs_ShouldLogError");
        }

        private static (TestCard card, TestGameCommandProcessor commandProcessor, TestLogger logger) SetupTest()
        {
            // Setup services
            var logger = new TestLogger();
            var commandProcessor = new TestGameCommandProcessor();
            ServiceLocator.RegisterService<ILogger>(logger);
            ServiceLocator.RegisterService<IGameCommandProcessor>(commandProcessor);

            // Create test card resource with actions
            var testCardResource = CreateTestCardResource();
            
            // Create test card
            var card = new TestCard();
            card.SetupForTesting(testCardResource, "test-card-1");

            return (card, commandProcessor, logger);
        }

        private static IGameStateData CreateGameStateWithInactiveSpell()
        {
            return GameState.Create(
                new CardsState(),
                new HandState(),
                new PlayerState(),
                new GamePhaseState(),
                SpellState.CreateInitial(), // Inactive spell
                StatusEffectsState.CreateInitial()
            );
        }

        private static IGameStateData CreateGameStateWithActiveSpell(int actionIndex, bool includeTestCard = true, SpellCardResource cardResource = null)
        {
            var playedCards = includeTestCard 
                ? new[] { CreateTestCardState(cardResource ?? CreateTestCardResource()) }
                : Array.Empty<CardState>();

            var cardsState = new CardsState(playedCards);

            var spellState = SpellState.CreateInitial()
                .WithActiveSpell(DateTime.UtcNow)
                .WithActionIndex(actionIndex);

            return GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                new GamePhaseState(),
                spellState,
                StatusEffectsState.CreateInitial()
            );
        }

        private static CardState CreateTestCardState(SpellCardResource resource)
        {
            return new CardState(
                cardId: "test-card-1",
                resource: resource,
                position: 0,
                containerType: ContainerType.PlayedCards
            );
        }

        private static SpellCardResource CreateTestCardResource()
        {
            var resource = new SpellCardResource();
            var action = new TestActionResource
            {
                PopupText = "Test Popup Text",
                Color = Colors.Red
            };
            resource.Actions = new Godot.Collections.Array<ActionResource> { action };
            return resource;
        }

        private static SpellCardResource CreateTestCardResourceWithMultipleActions()
        {
            var resource = new SpellCardResource();
            var action1 = new TestActionResource
            {
                PopupText = "First Action Text",
                Color = Colors.Red
            };
            var action2 = new TestActionResource
            {
                PopupText = "Second Action Text",
                Color = Colors.Blue
            };
            resource.Actions = new Godot.Collections.Array<ActionResource> { action1, action2 };
            return resource;
        }

        // Helper assertion methods
        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
        }
    }

    // Test implementations
    public partial class TestCard : global::Card
    {
        public bool PopupEffectShown { get; private set; }
        public string LastPopupText { get; private set; }
        public Color LastPopupColor { get; private set; }
        public bool ShouldThrowException { get; set; }

        public void SetupForTesting(SpellCardResource resource, string cardId)
        {
            Resource = resource;
            CardId = cardId;
        }

        public void TestOnGameStateChanged(IGameStateData previousState, IGameStateData newState)
        {
            if (ShouldThrowException)
            {
                throw new InvalidOperationException("Test exception");
            }

            // Call the internal method directly
            ActionActivationOnGameStateChanged(previousState, newState);
        }

        protected override void ShowPopupEffectForAction(ActionResource action, IGameStateData gameState)
        {
            PopupEffectShown = true;
            LastPopupText = action.GetPopUpEffectText(gameState);
            LastPopupColor = action.PopUpEffectColor;
        }
    }

    public partial class TestActionResource : ActionResource
    {
        public string PopupText { get; set; }
        public Color Color { get; set; }

        public override Color PopUpEffectColor => Color;

        public override string GetPopUpEffectText(IGameStateData gameState)
        {
            return PopupText;
        }

        public override GameCommand CreateExecutionCommand(string cardId)
        {
            return new TestCommand();
        }
    }

    public class TestCommand : GameCommand
    {
        public override bool CanExecute() => true;
        public override string GetDescription() => "Test command";
        public override void Execute(CommandCompletionToken token)
        {
            token.Complete(CommandResult.Success(ServiceLocator.GetService<IGameCommandProcessor>().CurrentState));
        }
    }

    public class TestGameCommandProcessor : IGameCommandProcessor
    {
        public IGameStateData CurrentState { get; private set; } = GameState.CreateInitial();
        public event Action<IGameStateData, IGameStateData> StateChanged;

        public bool ExecuteCommand(GameCommand command) => true;
        public void SetState(IGameStateData newState) 
        {
            var previousState = CurrentState;
            CurrentState = newState;
            StateChanged?.Invoke(previousState, newState);
        }
        public void NotifyBlockingCommandFinished() { }
    }

    public class TestLogger : ILogger
    {
        public bool ErrorLogged { get; private set; }
        public string LastErrorMessage { get; private set; }

        public void LogError(string message)
        {
            ErrorLogged = true;
            LastErrorMessage = message;
        }

        public void LogError(string message, Exception exception)
        {
            ErrorLogged = true;
            LastErrorMessage = message;
        }

        public void LogWarning(string message) { }
        public void LogInfo(string message) { }
        public void LogDebug(string message) { }
    }
}