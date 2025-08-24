using System;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for StatusEffectInstanceData class
    /// </summary>
    public static class StatusEffectInstanceDataTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running StatusEffectInstanceData tests...");
            
            TestConstructor();
            TestFromResource();
            TestWithStacks();
            TestWithAddedStacks();
            TestWithReducedStacks();
            TestIsExpired();
            TestShouldDecay();
            TestShouldTrigger();
            TestIsValid();
            TestEquality();
            TestToString();
            
            GD.Print("All StatusEffectInstanceData tests passed!");
        }

        private static void TestConstructor()
        {
            var resource = CreateTestStatusEffectResource();
            var appliedAt = DateTime.UtcNow;
            var instance = new StatusEffectInstanceData(
                StatusEffectType.Poison,
                3,
                resource,
                appliedAt);
            
            Assert(instance.EffectType == StatusEffectType.Poison, "Effect type should be Poison");
            Assert(instance.CurrentStacks == 3, "Current stacks should be 3");
            Assert(ReferenceEquals(instance.EffectResource, resource), "Effect resource should match");
            Assert(instance.AppliedAt == appliedAt, "Applied time should match");
            Assert(!instance.IsExpired, "Instance with stacks should not be expired");
        }

        private static void TestFromResource()
        {
            var resource = CreateTestStatusEffectResource();
            
            // Test with default parameters
            var instance1 = StatusEffectInstanceData.FromResource(resource);
            Assert(instance1.EffectType == StatusEffectType.Poison, "Effect type should match resource");
            Assert(instance1.CurrentStacks == resource.InitialStacks, "Should use resource initial stacks");
            Assert(ReferenceEquals(instance1.EffectResource, resource), "Effect resource should match");
            Assert(instance1.AppliedAt <= DateTime.UtcNow, "Applied time should be recent");
            
            // Test with custom parameters
            var customTime = DateTime.UtcNow.AddMinutes(-5);
            var instance2 = StatusEffectInstanceData.FromResource(resource, 5, customTime);
            Assert(instance2.CurrentStacks == 5, "Should use custom stacks");
            Assert(instance2.AppliedAt == customTime, "Should use custom applied time");
            
            // Test null resource throws exception
            try
            {
                StatusEffectInstanceData.FromResource(null);
                Assert(false, "Should throw exception for null resource");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        private static void TestWithStacks()
        {
            var resource = CreateTestStatusEffectResource();
            var instance = StatusEffectInstanceData.FromResource(resource, 3);
            
            // Test normal stack update
            var updated1 = instance.WithStacks(5);
            Assert(updated1.CurrentStacks == 5, "Stacks should be updated to 5");
            Assert(updated1.EffectType == instance.EffectType, "Other properties should remain same");
            Assert(updated1.AppliedAt == instance.AppliedAt, "Applied time should remain same");
            
            // Test stack clamping to max
            var updated2 = instance.WithStacks(150);
            Assert(updated2.CurrentStacks == resource.MaxStacks, "Stacks should be clamped to max");
            
            // Test negative stacks clamped to 0
            var updated3 = instance.WithStacks(-5);
            Assert(updated3.CurrentStacks == 0, "Negative stacks should be clamped to 0");
        }

        private static void TestWithAddedStacks()
        {
            var resource = CreateTestStatusEffectResource();
            var instance = StatusEffectInstanceData.FromResource(resource, 3);
            
            // Test adding stacks
            var updated1 = instance.WithAddedStacks(2);
            Assert(updated1.CurrentStacks == 5, "Should add 2 stacks (3 + 2 = 5)");
            
            // Test adding stacks with max limit
            var updated2 = instance.WithAddedStacks(100);
            Assert(updated2.CurrentStacks == resource.MaxStacks, "Should be clamped to max stacks");
            
            // Test adding negative stacks
            var updated3 = instance.WithAddedStacks(-2);
            Assert(updated3.CurrentStacks == 1, "Should subtract 2 stacks (3 - 2 = 1)");
        }

        private static void TestWithReducedStacks()
        {
            var resource = CreateTestStatusEffectResource();
            var instance = StatusEffectInstanceData.FromResource(resource, 4);
            
            // Test reducing stacks by custom amount
            var updated2 = instance.WithReducedStacks(3);
            Assert(updated2.CurrentStacks == 2, "Should reduce by 3 stacks (5 - 3 = 2)");
            
            // Test reducing more stacks than available
            var updated3 = instance.WithReducedStacks(10);
            Assert(updated3.CurrentStacks == 0, "Should be clamped to 0 stacks");
        }

        private static void TestIsExpired()
        {
            var resource = CreateTestStatusEffectResource();
            
            var activeInstance = StatusEffectInstanceData.FromResource(resource, 3);
            Assert(!activeInstance.IsExpired, "Instance with stacks should not be expired");
            
            var expiredInstance = StatusEffectInstanceData.FromResource(resource, 0);
            Assert(expiredInstance.IsExpired, "Instance with 0 stacks should be expired");
        }

        private static void TestShouldDecay()
        {
            var resource = CreateTestStatusEffectResource();
            resource.DecayMode = StatusEffectDecayMode.EndOfTurn;
            var instance = StatusEffectInstanceData.FromResource(resource);
            
            Assert(instance.ShouldDecay(StatusEffectDecayMode.EndOfTurn), "Should decay for matching decay mode");
            Assert(!instance.ShouldDecay(StatusEffectDecayMode.Never), "Should not decay for different decay mode");
        }

        private static void TestShouldTrigger()
        {
            var resource = CreateTestStatusEffectResource();
            resource.Trigger = StatusEffectTrigger.StartOfTurn;
            var instance = StatusEffectInstanceData.FromResource(resource);
            
            Assert(instance.ShouldTrigger(StatusEffectTrigger.StartOfTurn), "Should trigger for matching trigger");
            Assert(!instance.ShouldTrigger(StatusEffectTrigger.EndOfTurn), "Should not trigger for different trigger");
        }

        private static void TestIsValid()
        {
            var resource = CreateTestStatusEffectResource();
            
            // Valid instance
            var validInstance = StatusEffectInstanceData.FromResource(resource, 3);
            Assert(validInstance.IsValid(), "Valid instance should pass validation");
            
            // Test with null resource (should be invalid, but we can't easily create this without reflection)
            // Test with mismatched effect type (would require creating invalid state)
            // Test with negative stacks (should be prevented by WithStacks method)
            // Test with future applied time
            var futureTime = DateTime.UtcNow.AddMinutes(10);
            var futureInstance = new StatusEffectInstanceData(
                StatusEffectType.Poison,
                3,
                resource,
                futureTime);
            Assert(!futureInstance.IsValid(), "Instance with future applied time should be invalid");
        }

        private static void TestEquality()
        {
            var resource = CreateTestStatusEffectResource();
            var appliedAt = DateTime.UtcNow;
            
            var instance1 = new StatusEffectInstanceData(StatusEffectType.Poison, 3, resource, appliedAt);
            var instance2 = new StatusEffectInstanceData(StatusEffectType.Poison, 3, resource, appliedAt);
            var instance3 = new StatusEffectInstanceData(StatusEffectType.Poison, 4, resource, appliedAt);
            
            Assert(instance1.Equals(instance2), "Instances with same properties should be equal");
            Assert(!instance1.Equals(instance3), "Instances with different stacks should not be equal");
            Assert(!instance1.Equals(null), "Instance should not equal null");
            Assert(!instance1.Equals("string"), "Instance should not equal different type");
        }

        private static void TestToString()
        {
            var resource = CreateTestStatusEffectResource();
            var instance = StatusEffectInstanceData.FromResource(resource, 3);
            
            var str = instance.ToString();
            Assert(str.Contains("Poison"), "ToString should contain effect type");
            Assert(str.Contains("x3"), "ToString should contain stack count");
        }

        private static StatusEffectResource CreateTestStatusEffectResource()
        {
            var resource = new StatusEffectResource();
            resource.EffectType = StatusEffectType.Poison;
            resource.EffectName = "Test Poison";
            resource.Description = "Test poison effect";
            resource.Trigger = StatusEffectTrigger.StartOfTurn;
            resource.DecayMode = StatusEffectDecayMode.ReduceByOneEndOfTurn;
            resource.InitialStacks = 1;
            resource.Value = 2.0f;
            resource.MaxStacks = 10;
            return resource;
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