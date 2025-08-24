using System;
using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Spell;
using Maximagus.Scripts.Managers;
using Scripts.Utilities;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class StatusEffectActionResource : ActionResource
    {
        [Export] public StatusEffectResource StatusEffect { get; set; }
        [Export] public StatusEffectActionType ActionType { get; set; }
        [Export] public int Stacks { get; set; } = 1;

        public StatusEffectActionResource()
        {
            ResourceLocalToScene = true;
        }

        public override string GetPopUpEffectText(IGameStateData gameState)
        {
            try
            {
                // For Add and Remove actions, the popup text is static and doesn't need snapshots
                if (ActionType == StatusEffectActionType.Add)
                {
                    return $"+{Stacks} {StatusEffect.EffectType}";
                }
                else if (ActionType == StatusEffectActionType.Remove)
                {
                    return $"-{Stacks} {StatusEffect.EffectType}";
                }
                else if (ActionType == StatusEffectActionType.Set)
                {
                    // For Set actions, we need to show how many stacks are being removed
                    // This requires checking the state before the action was applied
                    
                    // Try to get snapshot-based information
                    var snapshot = SnapshotLookupHelper.TryGetSnapshotForAction(gameState, ActionId, "StatusEffectActionResource");
                    
                    if (snapshot != null)
                    {
                        // Get the status effect stacks from the resulting state (after the action)
                        var stacksAfter = StatusEffectLogicManager.GetStacksOfEffect(
                            snapshot.ResultingState.StatusEffects, StatusEffect.EffectType);
                        
                        // Get the stacks from the current game state (which should be the same as after)
                        var stacksBefore = StatusEffectLogicManager.GetStacksOfEffect(
                            gameState.StatusEffects, StatusEffect.EffectType);
                        
                        // For Set action, we're setting to a specific value, so show the change
                        var stacksRemoved = Math.Max(0, stacksBefore - Stacks);
                        
                        GD.Print($"[StatusEffectActionResource] Set action: before={stacksBefore}, after={stacksAfter}, removing={stacksRemoved}");
                        
                        if (stacksRemoved > 0)
                        {
                            return $"-{stacksRemoved} {StatusEffect.EffectType}";
                        }
                        else
                        {
                            return $"={Stacks} {StatusEffect.EffectType}";
                        }
                    }
                    
                    // Fallback to current state calculation
                    var currentStacks = StatusEffectLogicManager.GetStacksOfEffect(gameState.StatusEffects, StatusEffect.EffectType);
                    var stacksToRemove = Math.Max(0, currentStacks - Stacks);
                    
                    GD.Print($"[StatusEffectActionResource] Using fallback for Set action: current={currentStacks}, removing={stacksToRemove}");
                    
                    if (stacksToRemove > 0)
                    {
                        return $"-{stacksToRemove} {StatusEffect.EffectType}";
                    }
                    else
                    {
                        return $"={Stacks} {StatusEffect.EffectType}";
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                GD.Print($"[StatusEffectActionResource] Error getting popup text: {ex.Message}");
                
                // Fallback to simple display
                return ActionType switch
                {
                    StatusEffectActionType.Add => $"+{Stacks} {StatusEffect.EffectType}",
                    StatusEffectActionType.Remove => $"-{Stacks} {StatusEffect.EffectType}",
                    StatusEffectActionType.Set => $"={Stacks} {StatusEffect.EffectType}",
                    _ => string.Empty
                };
            }
        }

        public override Color PopUpEffectColor => StatusEffect.EffectType switch
        {
            StatusEffectType.Poison => new Color(1, 0.5f, 0),
            StatusEffectType.Chill => new Color(0, 0.5f, 1),
            StatusEffectType.Burning => new Color(0, 0.5f, 1),
            StatusEffectType.Bleeding => new Color(0, 0.5f, 1),
            _ => new Color(1, 1, 1)
        };

        public override GameCommand CreateExecutionCommand(string cardId)
        {
            return new ApplyStatusEffectCommand(StatusEffect, Stacks, ActionType);
        }
    }
}
