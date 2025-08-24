using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Spells.Abstractions;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for SpellHistoryEntry class
    /// </summary>
    public static class SpellHistoryEntryTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running SpellHistoryEntry tests...");
            
            TestConstructor();
            TestCreateSuccessful();
            TestCreateFailed();
            TestGetProperty();
            TestGetDuration();
            TestIsValid();
            TestEquality();
            TestToString();
            
            GD.Print("All SpellHistoryEntry tests passed!");
        }

        private static void TestConstructor()
        {
            var completedAt = DateTime.UtcNow;
            var properties = ImmutableDictionary<string, Variant>.Empty
                .Add("FireDamage", Variant.From(10f));
            var cardIds = ImmutableArray.Create("card1", "card2");
            
            var entry = new SpellHistoryEntry(
                completedAt,
                25.5f,
                properties,
                cardIds,
                ImmutableArray<SpellCardResource>.Empty,
                true,
                null);
            
            Assert(entry.CompletedAt == completedAt, "CompletedAt should match");
            Assert(Math.Abs(entry.TotalDamage - 25.5f) < 0.001f, "TotalDamage should be 25.5");
            Assert(entry.FinalProperties.Count == 1, "Should have 1 property");
            Assert(entry.CastCardIds.Length == 2, "Should have 2 card IDs");
            Assert(entry.CastCardResources.IsEmpty, "Should have empty card resources");
            Assert(entry.WasSuccessful == true, "Should be successful");
            Assert(entry.ErrorMessage == "", "Should have empty error message");
        }

        private static void TestCreateSuccessful()
        {
            var spellState = SpellState.CreateInitial()
                .WithActiveSpell(DateTime.UtcNow)
                .WithTotalDamage(15f)
                .WithProperty("TestProp", Variant.From(42));
            
            var cardIds = ImmutableArray.Create("card1", "card2");
            var cardResources = ImmutableArray<SpellCardResource>.Empty;
            
            var entry = SpellHistoryEntry.CreateSuccessful(spellState, cardIds, cardResources);
            
            Assert(entry.WasSuccessful == true, "Should be successful");
            Assert(Math.Abs(entry.TotalDamage - 15f) < 0.001f, "Should have correct total damage");
            Assert(entry.CastCardIds.SequenceEqual(cardIds), "Should have correct card IDs");
            Assert(entry.CastCardResources.SequenceEqual(cardResources), "Should have correct card resources");
            Assert(entry.ErrorMessage == "", "Should have empty error message");
            Assert(entry.GetProperty("TestProp", 0) == 42, "Should preserve properties");
            Assert(entry.IsValid(), "Should be valid");
        }

        private static void TestCreateFailed()
        {
            var spellState = SpellState.CreateInitial()
                .WithActiveSpell(DateTime.UtcNow)
                .WithTotalDamage(5f);
            
            var cardIds = ImmutableArray.Create("card1");
            var cardResources = ImmutableArray<SpellCardResource>.Empty;
            var errorMessage = "Test error occurred";
            
            var entry = SpellHistoryEntry.CreateFailed(spellState, cardIds, cardResources, errorMessage);
            
            Assert(entry.WasSuccessful == false, "Should not be successful");
            Assert(Math.Abs(entry.TotalDamage - 5f) < 0.001f, "Should have correct total damage");
            Assert(entry.CastCardIds.SequenceEqual(cardIds), "Should have correct card IDs");
            Assert(entry.CastCardResources.SequenceEqual(cardResources), "Should have correct card resources");
            Assert(entry.ErrorMessage == errorMessage, "Should have correct error message");
            Assert(entry.IsValid(), "Should be valid");
        }

        private static void TestGetProperty()
        {
            var properties = ImmutableDictionary<string, Variant>.Empty
                .Add("IntProp", Variant.From(42))
                .Add("FloatProp", Variant.From(3.14f))
                .Add("StringProp", Variant.From("test"));
            
            var entry = new SpellHistoryEntry(
                DateTime.UtcNow,
                10f,
                properties,
                ImmutableArray<string>.Empty,
                ImmutableArray<SpellCardResource>.Empty,
                true);
            
            Assert(entry.GetProperty("IntProp", 0) == 42, "Int property should be 42");
            Assert(Math.Abs(entry.GetProperty("FloatProp", 0f) - 3.14f) < 0.001f, "Float property should be 3.14");
            Assert(entry.GetProperty("StringProp", "") == "test", "String property should be 'test'");
            Assert(entry.GetProperty("NonExistent", 99) == 99, "Non-existent property should return default");
        }

        private static void TestGetDuration()
        {
            var completedAt = DateTime.UtcNow;
            var startTime = completedAt.AddMinutes(-5);
            
            var entry = new SpellHistoryEntry(completedAt, 10f);
            
            var duration = entry.GetDuration(startTime);
            Assert(duration.HasValue, "Duration should have value when start time provided");
            Assert(Math.Abs(duration.Value.TotalMinutes - 5) < 0.1, "Duration should be approximately 5 minutes");
            
            var noDuration = entry.GetDuration(null);
            Assert(!noDuration.HasValue, "Duration should be null when no start time provided");
        }

        private static void TestIsValid()
        {
            var now = DateTime.UtcNow;
            
            // Valid successful entry
            var validSuccess = new SpellHistoryEntry(now, 10f, null, 
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, true, null);
            Assert(validSuccess.IsValid(), "Valid successful entry should be valid");
            
            // Valid failed entry
            var validFailed = new SpellHistoryEntry(now, 5f, null,
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, false, "Error occurred");
            Assert(validFailed.IsValid(), "Valid failed entry should be valid");
            
            // Invalid: future completion time
            var futureEntry = new SpellHistoryEntry(now.AddMinutes(10), 10f, null,
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, true, null);
            Assert(!futureEntry.IsValid(), "Future completion time should be invalid");
            
            // Invalid: negative damage
            var negativeDamage = new SpellHistoryEntry(now, -5f, null,
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, true, null);
            Assert(!negativeDamage.IsValid(), "Negative damage should be invalid");
            
            // Invalid: mismatched card arrays
            var mismatchedCards = new SpellHistoryEntry(now, 10f, null,
                ImmutableArray.Create("card1", "card2"), ImmutableArray.Create((SpellCardResource)null), true, null);
            Assert(!mismatchedCards.IsValid(), "Mismatched card arrays should be invalid");
            
            // Invalid: failed without error message
            var failedNoError = new SpellHistoryEntry(now, 10f, null,
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, false, null);
            Assert(!failedNoError.IsValid(), "Failed entry without error message should be invalid");
            
            // Invalid: successful with error message
            var successWithError = new SpellHistoryEntry(now, 10f, null,
                ImmutableArray<string>.Empty, ImmutableArray<SpellCardResource>.Empty, true, "Error");
            Assert(!successWithError.IsValid(), "Successful entry with error message should be invalid");
        }

        private static void TestEquality()
        {
            var completedAt = DateTime.UtcNow;
            var properties = ImmutableDictionary<string, Variant>.Empty.Add("test", Variant.From(42));
            var cardIds = ImmutableArray.Create("card1");
            
            var entry1 = new SpellHistoryEntry(completedAt, 10f, properties, cardIds,
                ImmutableArray<SpellCardResource>.Empty, true, null);
            var entry2 = new SpellHistoryEntry(completedAt, 10f, properties, cardIds,
                ImmutableArray<SpellCardResource>.Empty, true, null);
            var entry3 = new SpellHistoryEntry(completedAt, 11f, properties, cardIds,
                ImmutableArray<SpellCardResource>.Empty, true, null);
            
            Assert(entry1.Equals(entry2), "Identical entries should be equal");
            Assert(!entry1.Equals(entry3), "Different entries should not be equal");
            Assert(!entry1.Equals(null), "Entry should not equal null");
            Assert(!entry1.Equals("string"), "Entry should not equal different type");
            
            // Test hash codes
            Assert(entry1.GetHashCode() == entry2.GetHashCode(), "Equal entries should have same hash code");
        }

        private static void TestToString()
        {
            var entry = new SpellHistoryEntry(
                DateTime.UtcNow,
                15.5f,
                ImmutableDictionary<string, Variant>.Empty,
                ImmutableArray.Create("card1", "card2"),
                ImmutableArray<SpellCardResource>.Empty,
                true,
                null);
            
            var str = entry.ToString();
            Assert(str.Contains("15.5"), "ToString should contain damage");
            Assert(str.Contains("2"), "ToString should contain card count");
            Assert(str.Contains("Success"), "ToString should contain success status");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}