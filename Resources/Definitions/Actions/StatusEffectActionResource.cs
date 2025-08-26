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
            return ActionType switch
            {
                StatusEffectActionType.Add => $"{StatusEffect.EffectType} +{Stacks}",
                StatusEffectActionType.Remove => $"{StatusEffect.EffectType} -{Stacks}",
                StatusEffectActionType.Set => $"{StatusEffect.EffectType} ={Stacks}",
                _ => string.Empty
            };
        }

        public override Color PopUpEffectColor => StatusEffect.EffectType switch
        {
            StatusEffectType.Burning => ElementColors.Fire,
            StatusEffectType.Chill => ElementColors.Frost,
            _ => ElementColors.Neutral
        };

        public override GameCommand CreateExecutionCommand(string cardId)
        {
            return new ApplyStatusEffectCommand(StatusEffect, Stacks, ActionType);
        }
    }
}
