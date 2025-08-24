using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Game;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Managers;

namespace Tests.Commands
{
    /// <summary>
    /// Comprehensive integration tests for the complete spell state migration system.
    /// Tests end-to-end spell casting, status effects, modifiers, history, and visual effects.
    /// </summary>
    public static class ComprehensiveIntegrationTests
    {
        private static IGameCommandProcessor _commandProcessor;
        private static ILogger _logger;

        public static void RunAllTests()
        {
            GD.Print("=== Running Comprehensive Integration Tests ===");
            
            Setup();
            
            // Core integration tests
            TestCompleteSpellCastingWithMultipleCards();
            TestSpellCastingWithStatusEffects();
            TestModifierApplicationAndConsumption();
            TestSpellHistoryRecording();
            TestVisualEffectsIntegration();
            TestRegressionSpellDamageCalculation();
            
            GD.Print("=== All Comprehensive Integration Tests Passed! ===");
        }

        private static void Setup()
        {
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _logger = ServiceLocator.GetService<ILogger>();
        }

        private static void TestCompleteSpellCastingWithMultipleCards()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing complete spell casting with multiple cards...");
            
            // Arrange: Create a spell with 3 different cards
            var card1 = CreateDamageCard("fire-bolt", "Fire Bolt", DamageType.Fire, 10);
            var card2 = CreateModifierCard("fire-boost", "Fire Boost", ModifierType.Add, DamageType.Fire, 5);
            var card3 = CreateDamageCard("frost-shard", "Frost Shard", DamageType.Frost, 8);
            
            var initialState = CreateGameStateWithPlayedCards(card1, card2, card3);
            _commandProcessor.SetState(initialState);
            
            // Act: Execute complete spell casting flow
            var results = ExecuteCompleteSpellCast();
            
            // Assert: Verify complete spell execution
            AssertTrue(results.Count >= 6, "Should have executed at least 6 commands");
            
            var finalState = _commandProcessor.CurrentState;
            AssertFalse(finalState.Spell.IsActive, "Spell should not be active after completion");
            AssertTrue(finalState.Spell.History.Length > 0, "Should have spell history entry");
            
            var historyEntry = finalState.Spell.History.Last();
            AssertTrue(historyEntry.WasSuccessful, "Spell should be successful");
            AssertEqual(3, historyEntry.CastCardIds.Length, "Should record all 3 cast cards");
            AssertTrue(historyEntry.TotalDamage > 0, "Should record total damage dealt");
            
            GD.Print("✓ Complete spell casting with multiple cards test passed");
        }

        private static void TestSpellCastingWithStatusEffects()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing spell casting with status effects...");
            
            // Arrange: Create spell with status effect and damage cards
            var statusCard = CreateStatusEffectCard("poison-dart", "Poison Dart", StatusEffectType.Poison, 2);
            var damageCard = CreateDamageCard("fire-bolt", "Fire Bolt", DamageType.Fire, 10);
            
            var initialState = CreateGameStateWithPlayedCards(statusCard, damageCard);
            _commandProcessor.SetState(initialState);
            
            // Act: Execute spell
            ExecuteCompleteSpellCast();
            
            // Assert: Verify status effect was applied
            var finalState = _commandProcessor.CurrentState;
            var poisonStacks = StatusEffectLogicManager.GetStacksOfEffect(finalState.StatusEffects, StatusEffectType.Poison);
            AssertEqual(2, poisonStacks, "Should have 2 stacks of poison");
            
            var historyEntry = finalState.Spell.History.Last();
            AssertEqual(10f, historyEntry.TotalDamage, "Should record damage from fire bolt");
            
            GD.Print("✓ Spell casting with status effects test passed");
        }

        private static void TestModifierApplicationAndConsumption()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing modifier application and consumption...");
            
            // Arrange: Create spell with consumable modifier and damage
            var modifier = CreateModifierCard("fire-boost", "Fire Boost", ModifierType.Add, DamageType.Fire, 5, true);
            var damage1 = CreateDamageCard("fire-bolt-1", "Fire Bolt 1", DamageType.Fire, 10);
            var damage2 = CreateDamageCard("fire-bolt-2", "Fire Bolt 2", DamageType.Fire, 10);
            
            var initialState = CreateGameStateWithPlayedCards(modifier, damage1, damage2);
            _commandProcessor.SetState(initialState);
            
            // Act: Execute spell
            ExecuteCompleteSpellCast();
            
            // Assert: Verify modifier consumption
            var finalState = _commandProcessor.CurrentState;
            var historyEntry = finalState.Spell.History.Last();
            
            // First damage should get modifier (10+5=15), second should not (10)
            // Total: 15 + 10 = 25
            AssertEqual(25f, historyEntry.TotalDamage, "Should consume modifier on first damage only");
            
            GD.Print("✓ Modifier application and consumption test passed");
        }

        private static void TestSpellHistoryRecording()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing spell history recording...");
            
            // Arrange: Create spell with multiple cards
            var card1 = CreateDamageCard("fire-bolt", "Fire Bolt", DamageType.Fire, 10);
            var card2 = CreateModifierCard("fire-boost", "Fire Boost", ModifierType.Add, DamageType.Fire, 5);
            
            var initialState = CreateGameStateWithPlayedCards(card1, card2);
            _commandProcessor.SetState(initialState);
            
            // Act: Execute spell
            ExecuteCompleteSpellCast();
            
            // Assert: Verify history recording
            var finalState = _commandProcessor.CurrentState;
            AssertEqual(1, finalState.Spell.History.Length, "Should have one history entry");
            
            var historyEntry = finalState.Spell.History[0];
            AssertTrue(historyEntry.WasSuccessful, "Spell should be successful");
            AssertEqual(2, historyEntry.CastCardIds.Length, "Should record all card IDs");
            AssertTrue(historyEntry.CastCardIds.Contains("fire-bolt"), "Should contain fire-bolt ID");
            AssertTrue(historyEntry.CastCardIds.Contains("fire-boost"), "Should contain fire-boost ID");
            AssertTrue(historyEntry.TotalDamage > 0, "Should record total damage");
            
            GD.Print("✓ Spell history recording test passed");
        }

        private static void TestVisualEffectsIntegration()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing visual effects integration...");
            
            // Arrange: Create mock card to track visual effects
            var mockCard = new MockCard();
            var damageCard = CreateDamageCard("fire-bolt", "Fire Bolt", DamageType.Fire, 10);
            mockCard.SetupForTesting(damageCard, "fire-bolt");
            
            // Setup command processor with state change tracking
            var mockProcessor = new MockCommandProcessor();
            ServiceLocator.RegisterService<IGameCommandProcessor>(mockProcessor);
            
            var initialState = CreateGameStateWithPlayedCards(damageCard);
            mockProcessor.SetState(initialState);
            
            // Subscribe card to state changes
            mockProcessor.StateChanged += mockCard.OnGameStateChanged;
            
            // Act: Execute spell actions to trigger state changes
            var startCommand = new StartSpellCommand();
            var startResult = ExecuteCommandAndGetResult(startCommand, mockProcessor);
            mockProcessor.SetState(startResult.NewState);
            
            var executeCommand = new ExecuteCardActionCommand(damageCard.Actions[0], "fire-bolt");
            var executeResult = ExecuteCommandAndGetResult(executeCommand, mockProcessor);
            mockProcessor.SetState(executeResult.NewState);
            
            // Assert: Verify visual effects were triggered
            AssertTrue(mockCard.PopupEffectShown, "Popup effect should be shown when action index advances");
            
            GD.Print("✓ Visual effects integration test passed");
        }

        private static void TestRegressionSpellDamageCalculation()
        {
            GD.Print("[ComprehensiveIntegrationTests] Testing regression: spell damage calculation...");
            
            // Test cases that should match original SpellContext behavior
            var testCases = new[]
            {
                new { Cards = new[] { CreateDamageCard("fire", "Fire", DamageType.Fire, 10) }, Expected = 10f },
                new { Cards = new[] { 
                    CreateModifierCard("boost", "Boost", ModifierType.Add, DamageType.Fire, 5, true),
                    CreateDamageCard("fire", "Fire", DamageType.Fire, 10) 
                }, Expected = 15f },
                new { Cards = new[] { 
                    CreateModifierCard("amp", "Amp", ModifierType.Multiply, DamageType.Fire, 2f, true),
                    CreateDamageCard("fire", "Fire", DamageType.Fire, 10) 
                }, Expected = 20f }
            };
            
            foreach (var testCase in testCases)
            {
                var initialState = CreateGameStateWithPlayedCards(testCase.Cards);
                _commandProcessor.SetState(initialState);
                
                ExecuteCompleteSpellCast();
                
                var finalState = _commandProcessor.CurrentState;
                var actualDamage = finalState.Spell.History.Last().TotalDamage;
                
                AssertEqual(testCase.Expected, actualDamage, 
                    $"Damage calculation regression test failed. Expected: {testCase.Expected}, Actual: {actualDamage}");
            }
            
            GD.Print("✓ Regression: spell damage calculation test passed");
        }

        #region Helper Methods

        private static List<CommandResult> ExecuteCompleteSpellCast(IGameCommandProcessor processor = null)
        {
            processor = processor ?? _commandProcessor;
            var results = new List<CommandResult>();
            
            var spellCastCommand = new SpellCastCommand();
            var spellCastResult = ExecuteCommandAndGetResult(spellCastCommand, processor);
            results.Add(spellCastResult);
            
            var currentState = spellCastResult.NewState;
            foreach (var command in spellCastResult.FollowUpCommands)
            {
                processor.SetState(currentState);
                var result = ExecuteCommandAndGetResult(command, processor);
                results.Add(result);
                currentState = result.NewState;
                
                if (result.FollowUpCommands != null)
                {
                    foreach (var followUp in result.FollowUpCommands)
                    {
                        processor.SetState(currentState);
                        var followUpResult = ExecuteCommandAndGetResult(followUp, processor);
                        results.Add(followUpResult);
                        currentState = followUpResult.NewState;
                    }
                }
            }
            
            processor.SetState(currentState);
            return results;
        }

        private static CommandResult ExecuteCommandAndGetResult(GameCommand command, IGameCommandProcessor processor = null)
        {
            processor = processor ?? _commandProcessor;
            
            var token = new CommandCompletionToken();
            var result = default(CommandResult);
            var completed = false;
            
            token.Subscribe((r) => {
                result = r;
                completed = true;
            });
            
            command.Execute(token);
            
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (!completed && DateTime.UtcNow < timeout)
            {
                System.Threading.Thread.Sleep(10);
            }
            
            if (!completed)
            {
                throw new TimeoutException($"Command {command.GetType().Name} did not complete within timeout");
            }
            
            return result;
        }

        private static IGameStateData CreateGameStateWithPlayedCards(params SpellCardResource[] cards)
        {
            var cardStates = cards.Select((card, index) => 
                new CardState(card.CardResourceId, card, false, false, false, index, ContainerType.PlayedCards)).ToArray();
            
            var cardsState = new CardsState(cardStates);
            var phaseState = new GamePhaseState().WithPhase(GamePhase.SpellCasting);
            
            return GameState.Create(
                cardsState,
                new HandState(),
                new PlayerState(),
                phaseState,
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );
        }

        private static SpellCardResource CreateDamageCard(string id, string name, DamageType damageType, int amount)
        {
            var action = new DamageActionResource
            {
                DamageType = damageType,
                Amount = amount
            };
            
            var card = new SpellCardResource();
            card.CardResourceId = id;
            card.CardName = name;
            card.Actions = new Godot.Collections.Array<ActionResource> { action };
            
            return card;
        }

        private static SpellCardResource CreateModifierCard(string id, string name, ModifierType type, DamageType element, float value, bool consumable = true)
        {
            var action = new ModifierActionResource
            {
                ModifierType = type,
                Element = element,
                Value = value,
                IsConsumedOnUse = consumable
            };
            
            var card = new SpellCardResource();
            card.CardResourceId = id;
            card.CardName = name;
            card.Actions = new Godot.Collections.Array<ActionResource> { action };
            
            return card;
        }

        private static SpellCardResource CreateStatusEffectCard(string id, string name, StatusEffectType effectType, int stacks)
        {
            var statusEffect = CreateStatusEffectResource(effectType, StatusEffectTrigger.StartOfTurn, StatusEffectDecayMode.Never, 1f);
            
            var action = new StatusEffectActionResource
            {
                StatusEffect = statusEffect,
                Stacks = stacks,
                ActionType = StatusEffectActionType.Add
            };
            
            var card = new SpellCardResource();
            card.CardResourceId = id;
            card.CardName = name;
            card.Actions = new Godot.Collections.Array<ActionResource> { action };
            
            return card;
        }

        private static StatusEffectResource CreateStatusEffectResource(StatusEffectType effectType, StatusEffectTrigger trigger, StatusEffectDecayMode decayMode, float value)
        {
            var resource = new StatusEffectResource();
            resource.EffectType = effectType;
            resource.Trigger = trigger;
            resource.DecayMode = decayMode;
            resource.Value = value;
            resource.EffectName = effectType.ToString();
            resource.Description = $"Test {effectType} effect";
            resource.InitialStacks = 1;
            
            return resource;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                _logger.LogError($"[ComprehensiveIntegrationTests] Assertion failed: {message}");
                throw new Exception($"Test assertion failed: {message}");
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                _logger.LogError($"[ComprehensiveIntegrationTests] Assertion failed: {message}");
                throw new Exception($"Test assertion failed: {message}");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                _logger.LogError($"[ComprehensiveIntegrationTests] Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
                throw new Exception($"Test assertion failed: {message}. Expected: {expected}, Actual: {actual}");
            }
        }

        #endregion

        #region Mock Classes

        private class MockCommandProcessor : IGameCommandProcessor
        {
            public IGameStateData CurrentState { get; private set; }
            public event Action<IGameStateData, IGameStateData> StateChanged;

            public MockCommandProcessor()
            {
                CurrentState = GameState.CreateInitial();
            }

            public bool ExecuteCommand(GameCommand command) => true;

            public void SetState(IGameStateData newState)
            {
                var previousState = CurrentState;
                CurrentState = newState;
                StateChanged?.Invoke(previousState, newState);
            }

            public void NotifyBlockingCommandFinished() { }
        }

        private class MockCard
        {
            public bool PopupEffectShown { get; private set; }
            public string LastPopupText { get; private set; }
            public Color LastPopupColor { get; private set; }
            
            protected SpellCardResource Resource { get; private set; }
            protected string CardId { get; private set; }

            public void SetupForTesting(SpellCardResource resource, string cardId)
            {
                Resource = resource;
                CardId = cardId;
            }

            public virtual void OnGameStateChanged(IGameStateData previousState, IGameStateData newState)
            {
                try
                {
                    if (!newState.Spell.IsActive) return;
                    
                    var playedCard = newState.Cards.PlayedCards.FirstOrDefault(c => c.CardId == CardId);
                    if (playedCard == null) return;
                    
                    if (newState.Spell.CurrentActionIndex > previousState.Spell.CurrentActionIndex)
                    {
                        ShowPopupEffectForAction(Resource.Actions[0], newState);
                    }
                }
                catch (Exception ex)
                {
                    var logger = ServiceLocator.GetService<ILogger>();
                    logger?.LogError($"Error handling state change for card {CardId}: {ex.Message}");
                }
            }

            protected virtual void ShowPopupEffectForAction(ActionResource action, IGameStateData gameState)
            {
                PopupEffectShown = true;
                LastPopupText = action.GetPopUpEffectText(gameState);
                LastPopupColor = action.PopUpEffectColor;
            }
        }

        #endregion
    }
}