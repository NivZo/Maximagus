using System;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;

namespace Tests.Resources
{
    /// <summary>
    /// Unit tests for StatusEffectResource functionality
    /// </summary>
    public static class StatusEffectResourceTests
    {
        public static void RunAllTests()
        {
            GD.Print("=== StatusEffectResource Tests ===");
            
            TestCalculateEffectValue();
            TestGetDisplayText();
            TestOnTrigger();
            TestIsValid();
            TestCreateInstance();
            TestToString();
            TestEdgeCases();
            
            GD.Print("=== StatusEffectResource Tests Complete ===");
        }

        private static void TestCalculateEffectValue()
        {
            GD.Print("Testing CalculateEffectValue...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            
            // Test basic calculation
            var result1 = poisonResource.CalculateEffectValue(1);
            Assert(result1 == 5.0f, $"Expected 5.0, got {result1}");
            
            var result2 = poisonResource.CalculateEffectValue(3);
            Assert(result2 == 15.0f, $"Expected 15.0, got {result2}");
            
            // Test zero stacks
            var result3 = poisonResource.CalculateEffectValue(0);
            Assert(result3 == 0.0f, $"Expected 0.0, got {result3}");
            
            GD.Print("✓ CalculateEffectValue tests passed");
        }

        private static void TestGetDisplayText()
        {
            GD.Print("Testing GetDisplayText...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 3.0f);
            var bleedingResource = CreateTestStatusEffectResource(StatusEffectType.Bleeding, 2.0f);
            var chillResource = CreateTestStatusEffectResource(StatusEffectType.Chill, 1.0f);
            var burningResource = CreateTestStatusEffectResource(StatusEffectType.Burning, 4.0f);
            
            // Test poison display text
            var poisonText = poisonResource.GetDisplayText(2);
            Assert(poisonText.Contains("Poison"), $"Poison text should contain 'Poison': {poisonText}");
            Assert(poisonText.Contains("6"), $"Poison text should contain damage value '6': {poisonText}");
            Assert(poisonText.Contains("2 stacks"), $"Poison text should contain '2 stacks': {poisonText}");
            
            // Test bleeding display text
            var bleedingText = bleedingResource.GetDisplayText(3);
            Assert(bleedingText.Contains("Bleeding"), $"Bleeding text should contain 'Bleeding': {bleedingText}");
            Assert(bleedingText.Contains("6"), $"Bleeding text should contain damage value '6': {bleedingText}");
            Assert(bleedingText.Contains("3 stacks"), $"Bleeding text should contain '3 stacks': {bleedingText}");
            
            // Test chill display text
            var chillText = chillResource.GetDisplayText(1);
            Assert(chillText.Contains("Chill"), $"Chill text should contain 'Chill': {chillText}");
            Assert(chillText.Contains("1"), $"Chill text should contain reduction value '1': {chillText}");
            
            // Test burning display text
            var burningText = burningResource.GetDisplayText(2);
            Assert(burningText.Contains("Burning"), $"Burning text should contain 'Burning': {burningText}");
            Assert(burningText.Contains("8"), $"Burning text should contain damage value '8': {burningText}");
            
            GD.Print("✓ GetDisplayText tests passed");
        }

        private static void TestOnTrigger()
        {
            GD.Print("Testing OnTrigger...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            
            // Test that OnTrigger doesn't throw exceptions
            try
            {
                poisonResource.OnTrigger(1);
                poisonResource.OnTrigger(3);
                poisonResource.OnTrigger(0);
            }
            catch (Exception ex)
            {
                Assert(false, $"OnTrigger should not throw exceptions: {ex.Message}");
            }
            
            GD.Print("✓ OnTrigger tests passed");
        }

        private static void TestIsValid()
        {
            GD.Print("Testing IsValid...");
            
            // Test valid resource
            var validResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            Assert(validResource.IsValid(), "Valid resource should return true for IsValid()");
            
            // Test invalid resources
            var emptyNameResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            emptyNameResource.EffectName = "";
            Assert(!emptyNameResource.IsValid(), "Resource with empty name should be invalid");
            
            var zeroInitialStacksResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            zeroInitialStacksResource.InitialStacks = 0;
            Assert(!zeroInitialStacksResource.IsValid(), "Resource with zero initial stacks should be invalid");
            
            var invalidMaxStacksResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            invalidMaxStacksResource.MaxStacks = 0; // Less than InitialStacks (1)
            Assert(!invalidMaxStacksResource.IsValid(), "Resource with MaxStacks < InitialStacks should be invalid");
            
            var negativeValueResource = CreateTestStatusEffectResource(StatusEffectType.Poison, -1.0f);
            Assert(!negativeValueResource.IsValid(), "Resource with negative value should be invalid");
            
            GD.Print("✓ IsValid tests passed");
        }

        private static void TestCreateInstance()
        {
            GD.Print("Testing CreateInstance...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            
            // Test creating instance with default stacks
            var instance1 = poisonResource.CreateInstance();
            Assert(instance1.EffectType == StatusEffectType.Poison, "Instance should have correct effect type");
            Assert(instance1.CurrentStacks == poisonResource.InitialStacks, "Instance should use InitialStacks by default");
            Assert(ReferenceEquals(instance1.EffectResource, poisonResource), "Instance should reference the original resource");
            
            // Test creating instance with specific stacks
            var instance2 = poisonResource.CreateInstance(3);
            Assert(instance2.CurrentStacks == 3, "Instance should use specified stacks");
            
            // Test creating instance with specific time
            var specificTime = DateTime.UtcNow.AddMinutes(-5);
            var instance3 = poisonResource.CreateInstance(2, specificTime);
            Assert(instance3.AppliedAt == specificTime, "Instance should use specified applied time");
            Assert(instance3.CurrentStacks == 2, "Instance should use specified stacks");
            
            GD.Print("✓ CreateInstance tests passed");
        }

        private static void TestToString()
        {
            GD.Print("Testing ToString...");
            
            var poisonResource = CreateTestStatusEffectResource(StatusEffectType.Poison, 5.0f);
            var toStringResult = poisonResource.ToString();
            
            Assert(toStringResult.Contains("StatusEffectResource"), "ToString should contain class name");
            Assert(toStringResult.Contains("Poison"), "ToString should contain effect type");
            Assert(toStringResult.Contains("5"), "ToString should contain value");
            Assert(toStringResult.Contains("1"), "ToString should contain initial stacks");
            Assert(toStringResult.Contains("99"), "ToString should contain max stacks");
            
            GD.Print("✓ ToString tests passed");
        }

        private static void TestEdgeCases()
        {
            GD.Print("Testing edge cases...");
            
            var resource = CreateTestStatusEffectResource(StatusEffectType.Poison, 0.0f);
            
            // Test zero value
            var zeroResult = resource.CalculateEffectValue(5);
            Assert(zeroResult == 0.0f, "Zero value should result in zero effect");
            
            // Test large stacks
            var largeResult = resource.CalculateEffectValue(1000);
            Assert(largeResult == 0.0f, "Large stacks with zero value should still be zero");
            
            // Test negative stacks (edge case)
            var negativeResult = resource.CalculateEffectValue(-1);
            Assert(negativeResult == 0.0f, "Negative stacks should result in zero or negative effect");
            
            GD.Print("✓ Edge case tests passed");
        }

        private static StatusEffectResource CreateTestStatusEffectResource(
            StatusEffectType effectType, 
            float value = 1.0f,
            StatusEffectTrigger trigger = StatusEffectTrigger.EndOfTurn,
            StatusEffectDecayMode decayMode = StatusEffectDecayMode.Never)
        {
            var resource = new StatusEffectResource();
            resource.EffectType = effectType;
            resource.EffectName = effectType.ToString();
            resource.Description = $"Test {effectType} effect";
            resource.Trigger = trigger;
            resource.DecayMode = decayMode;
            resource.InitialStacks = 1;
            resource.Value = value;
            resource.MaxStacks = 99;
            return resource;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                GD.PrintErr($"ASSERTION FAILED: {message}");
                throw new Exception($"Test assertion failed: {message}");
            }
        }
    }
}