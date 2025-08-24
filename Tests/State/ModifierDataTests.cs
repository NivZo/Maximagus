using System;
using System.Collections.Immutable;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Enums;

namespace Tests.State
{
    /// <summary>
    /// Unit tests for ModifierData class
    /// </summary>
    public static class ModifierDataTests
    {
        public static void RunAllTests()
        {
            GD.Print("Running ModifierData tests...");
            
            TestConstructor();
            TestFromActionResource();
            TestCanApply();
            TestApply();
            TestIsValid();
            TestEquality();
            TestToString();
            
            GD.Print("All ModifierData tests passed!");
        }

        private static void TestConstructor()
        {
            var conditions = ImmutableArray.Create(SpellModifierCondition.IsFire);
            var modifier = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                10f,
                true,
                conditions);
            
            Assert(modifier.Type == ModifierType.Add, "Type should be Add");
            Assert(modifier.Element == DamageType.Fire, "Element should be Fire");
            Assert(Math.Abs(modifier.Value - 10f) < 0.001f, "Value should be 10");
            Assert(modifier.IsConsumedOnUse == true, "Should be consumed on use");
            Assert(modifier.Conditions.Length == 1, "Should have 1 condition");
            Assert(modifier.Conditions[0] == SpellModifierCondition.IsFire, "Condition should be IsFire");
        }

        private static void TestFromActionResource()
        {
            // This test would require creating a mock ModifierActionResource
            // For now, we'll test the basic functionality
            var modifier = new ModifierData(
                ModifierType.Multiply,
                DamageType.Frost,
                2f,
                false);
            
            Assert(modifier.Type == ModifierType.Multiply, "Type should be Multiply");
            Assert(modifier.Element == DamageType.Frost, "Element should be Frost");
            Assert(Math.Abs(modifier.Value - 2f) < 0.001f, "Value should be 2");
            Assert(modifier.IsConsumedOnUse == false, "Should not be consumed on use");
            Assert(modifier.Conditions.IsEmpty, "Should have no conditions");
        }

        private static void TestCanApply()
        {
            // Test modifier with no conditions (should apply to everything)
            var noConditionsModifier = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                5f,
                true);
            
            Assert(noConditionsModifier.CanApply(DamageType.Fire), "No conditions modifier should apply to Fire");
            Assert(noConditionsModifier.CanApply(DamageType.Frost), "No conditions modifier should apply to Frost");
            Assert(noConditionsModifier.CanApply(DamageType.None), "No conditions modifier should apply to None");
            
            // Test modifier with Fire condition
            var fireConditionModifier = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                5f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            
            Assert(fireConditionModifier.CanApply(DamageType.Fire), "Fire condition modifier should apply to Fire");
            Assert(!fireConditionModifier.CanApply(DamageType.Frost), "Fire condition modifier should not apply to Frost");
            Assert(!fireConditionModifier.CanApply(DamageType.None), "Fire condition modifier should not apply to None");
            
            // Test modifier with Frost condition
            var frostConditionModifier = new ModifierData(
                ModifierType.Add,
                DamageType.Frost,
                5f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFrost));
            
            Assert(!frostConditionModifier.CanApply(DamageType.Fire), "Frost condition modifier should not apply to Fire");
            Assert(frostConditionModifier.CanApply(DamageType.Frost), "Frost condition modifier should apply to Frost");
            Assert(!frostConditionModifier.CanApply(DamageType.None), "Frost condition modifier should not apply to None");
            
            // Test modifier with multiple conditions (all must be met)
            var multiConditionModifier = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                5f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire, SpellModifierCondition.IsFrost));
            
            Assert(!multiConditionModifier.CanApply(DamageType.Fire), "Multi-condition modifier should not apply to Fire only");
            Assert(!multiConditionModifier.CanApply(DamageType.Frost), "Multi-condition modifier should not apply to Frost only");
        }

        private static void TestApply()
        {
            // Test Add modifier
            var addModifier = new ModifierData(ModifierType.Add, DamageType.Fire, 10f, true);
            Assert(Math.Abs(addModifier.Apply(5f) - 15f) < 0.001f, "Add modifier should add value");
            
            // Test Multiply modifier
            var multiplyModifier = new ModifierData(ModifierType.Multiply, DamageType.Fire, 2f, true);
            Assert(Math.Abs(multiplyModifier.Apply(5f) - 10f) < 0.001f, "Multiply modifier should multiply value");
            
            // Test Set modifier
            var setModifier = new ModifierData(ModifierType.Set, DamageType.Fire, 20f, true);
            Assert(Math.Abs(setModifier.Apply(5f) - 20f) < 0.001f, "Set modifier should set value");
            
            // Test with zero base damage
            Assert(Math.Abs(addModifier.Apply(0f) - 10f) < 0.001f, "Add modifier should work with zero base");
            Assert(Math.Abs(multiplyModifier.Apply(0f) - 0f) < 0.001f, "Multiply modifier should work with zero base");
            Assert(Math.Abs(setModifier.Apply(0f) - 20f) < 0.001f, "Set modifier should work with zero base");
        }

        private static void TestIsValid()
        {
            // Valid modifiers
            var validAdd = new ModifierData(ModifierType.Add, DamageType.Fire, 10f, true);
            Assert(validAdd.IsValid(), "Valid Add modifier should be valid");
            
            var validMultiply = new ModifierData(ModifierType.Multiply, DamageType.Fire, 2f, true);
            Assert(validMultiply.IsValid(), "Valid Multiply modifier should be valid");
            
            var validSet = new ModifierData(ModifierType.Set, DamageType.Fire, 15f, true);
            Assert(validSet.IsValid(), "Valid Set modifier should be valid");
            
            // Edge cases for Add (can be negative)
            var negativeAdd = new ModifierData(ModifierType.Add, DamageType.Fire, -5f, true);
            Assert(negativeAdd.IsValid(), "Negative Add modifier should be valid");
            
            // Invalid Multiply (should be positive)
            var zeroMultiply = new ModifierData(ModifierType.Multiply, DamageType.Fire, 0f, true);
            Assert(!zeroMultiply.IsValid(), "Zero Multiply modifier should be invalid");
            
            var negativeMultiply = new ModifierData(ModifierType.Multiply, DamageType.Fire, -1f, true);
            Assert(!negativeMultiply.IsValid(), "Negative Multiply modifier should be invalid");
            
            // Invalid Set (should be non-negative)
            var negativeSet = new ModifierData(ModifierType.Set, DamageType.Fire, -10f, true);
            Assert(!negativeSet.IsValid(), "Negative Set modifier should be invalid");
        }

        private static void TestEquality()
        {
            var modifier1 = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                10f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            
            var modifier2 = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                10f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            
            var modifier3 = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                11f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            
            Assert(modifier1.Equals(modifier2), "Identical modifiers should be equal");
            Assert(!modifier1.Equals(modifier3), "Different modifiers should not be equal");
            Assert(!modifier1.Equals(null), "Modifier should not equal null");
            Assert(!modifier1.Equals("string"), "Modifier should not equal different type");
            
            // Test hash codes
            Assert(modifier1.GetHashCode() == modifier2.GetHashCode(), "Equal modifiers should have same hash code");
        }

        private static void TestToString()
        {
            var modifier = new ModifierData(
                ModifierType.Add,
                DamageType.Fire,
                10f,
                true,
                ImmutableArray.Create(SpellModifierCondition.IsFire));
            
            var str = modifier.ToString();
            Assert(str.Contains("Add"), "ToString should contain modifier type");
            Assert(str.Contains("10"), "ToString should contain value");
            Assert(str.Contains("Fire"), "ToString should contain element");
            Assert(str.Contains("True"), "ToString should contain consumed flag");
            Assert(str.Contains("IsFire"), "ToString should contain conditions");
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