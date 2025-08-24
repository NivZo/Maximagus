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
                StatusEffectActionType.Add => $"+{Stacks} {StatusEffect.EffectType}",
                StatusEffectActionType.Remove => $"-{Stacks} {StatusEffect.EffectType}",
                StatusEffectActionType.Set => $"={Stacks} {StatusEffect.EffectType}",
                _ => string.Empty
            };
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
