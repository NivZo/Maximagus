using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.StatusEffects
{
    [GlobalClass]
    public partial class StatusEffectResource : Resource
    {
        [Export] public StatusEffectType EffectType { get; set; }
        [Export] public string EffectName { get; set; }
        [Export] public string Description { get; set; }
        [Export] public StatusEffectTrigger Trigger { get; set; }
        [Export] public StatusEffectDecayMode DecayMode { get; set; }
        [Export] public bool IsStackable { get; set; } = true;
        [Export] public int InitialStacks { get; set; } = 1;
        [Export] public float Value { get; set; }
        [Export] public int MaxStacks { get; set; } = 99;

        public virtual void OnTrigger(int stacks)
        {
            switch (EffectType)
            {
                case StatusEffectType.Poison:
                    var poisonDamage = Value * stacks;
                    GD.Print($"Poison deals {poisonDamage} damage ({stacks} stacks)");
                    break;
                    
                case StatusEffectType.Bleeding:
                    var bleedDamage = Value * stacks;
                    GD.Print($"Bleeding adds {bleedDamage} damage to attack ({stacks} stacks)");
                    break;
            }
        }
    }
}
