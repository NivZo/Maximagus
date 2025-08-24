using System;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;

namespace Tests
{
    /// <summary>
    /// Simple validation test for StatusEffectLogicManager
    /// </summary>
    public static class StatusEffectLogicManagerSimpleTest
    {
        public static void ValidateImplementation()
        {
            Console.WriteLine("=== Validating StatusEffectLogicManager Implementation ===");
            
            // Create a test status effect resource
            var poisonResource = new StatusEffectResource();
            poisonResource.EffectType = StatusEffectType.Poison;
            poisonResource.EffectName = "Poison";
            poisonResource.Description = "Test poison effect";
            poisonResource.Trigger = StatusEffectTrigger.EndOfTurn;
            poisonResource.DecayMode = StatusEffectDecayMode.ReduceByOneEndOfTurn;
            poisonResource.InitialStacks = 1;
            poisonResource.Value = 1.0f;
            poisonResource.MaxStacks = 10;
            
            // Test basic functionality
            var initialState = StatusEffectsState.CreateInitial();
            
            // Test ApplyStatusEffect
            var state1 = StatusEffectLogicManager.ApplyStatusEffect(initialState, poisonResource, 3, StatusEffectActionType.Add);
            if (state1.GetStacksOfEffect(StatusEffectType.Poison) != 3)
            {
                throw new Exception("ApplyStatusEffect failed - expected 3 poison stacks");
            }
            
            // Test GetStacksOfEffect
            var stacks = StatusEffectLogicManager.GetStacksOfEffect(state1, StatusEffectType.Poison);
            if (stacks != 3)
            {
                throw new Exception("GetStacksOfEffect failed - expected 3 stacks");
            }
            
            // Test HasEffect
            var hasPoison = StatusEffectLogicManager.HasEffect(state1, StatusEffectType.Poison);
            if (!hasPoison)
            {
                throw new Exception("HasEffect failed - should have poison effect");
            }
            
            var hasBleed = StatusEffectLogicManager.HasEffect(state1, StatusEffectType.Bleeding);
            if (hasBleed)
            {
                throw new Exception("HasEffect failed - should not have bleeding effect");
            }
            
            // Test ProcessDecay
            var decayedState = StatusEffectLogicManager.ProcessDecay(state1, StatusEffectDecayMode.ReduceByOneEndOfTurn);
            var stacksAfterDecay = StatusEffectLogicManager.GetStacksOfEffect(decayedState, StatusEffectType.Poison);
            if (stacksAfterDecay != 2)
            {
                throw new Exception("ProcessDecay failed - expected 2 stacks after decay");
            }
            
            // Test ValidateState
            var isValid = StatusEffectLogicManager.ValidateState(state1);
            if (!isValid)
            {
                throw new Exception("ValidateState failed - state should be valid");
            }
            
            var nullValid = StatusEffectLogicManager.ValidateState(null);
            if (nullValid)
            {
                throw new Exception("ValidateState failed - null state should be invalid");
            }
            
            // Test null argument handling
            try
            {
                StatusEffectLogicManager.ApplyStatusEffect(null, poisonResource, 1, StatusEffectActionType.Add);
                throw new Exception("Should have thrown ArgumentNullException for null state");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
            
            try
            {
                StatusEffectLogicManager.ApplyStatusEffect(initialState, null, 1, StatusEffectActionType.Add);
                throw new Exception("Should have thrown ArgumentNullException for null effect");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
            
            Console.WriteLine("✅ StatusEffectLogicManager implementation validation passed!");
        }
    }
}