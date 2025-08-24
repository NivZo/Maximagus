using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;

/// <summary>
/// Interface for status effect management using command-based approach.
/// All operations work through the centralized GameState and command system.
/// </summary>
public interface IStatusEffectManager
{
    /// <summary>
    /// Applies a status effect using the ApplyStatusEffectCommand.
    /// Updates the centralized StatusEffectsState through the command system.
    /// </summary>
    /// <param name="effect">The status effect resource to apply</param>
    /// <param name="stacks">Number of stacks to apply (default: 1)</param>
    /// <param name="actionType">How to apply the stacks (default: Add)</param>
    void AddStatusEffect(StatusEffectResource effect, int stacks = 1, StatusEffectActionType actionType = StatusEffectActionType.Add);

    /// <summary>
    /// Triggers status effects using the TriggerStatusEffectsCommand.
    /// Processes all active status effects that match the trigger condition.
    /// </summary>
    /// <param name="trigger">The trigger condition for status effects</param>
    void TriggerEffects(StatusEffectTrigger trigger);

    /// <summary>
    /// Gets the current stacks of a specific status effect type.
    /// Reads from the centralized StatusEffectsState.
    /// </summary>
    /// <param name="statusEffectType">The type of status effect to query</param>
    /// <returns>The number of stacks currently active</returns>
    int GetStacksOfEffect(StatusEffectType statusEffectType);
}