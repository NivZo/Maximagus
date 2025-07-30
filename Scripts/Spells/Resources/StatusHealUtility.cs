
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public partial class StatusHealUtility : UtilityCardResource
    {
        [Export] public float HealPerStatusEffect { get; set; } = 5f;

        public override void Execute(SpellContext context)
        {
            // This is a placeholder. The actual implementation will depend on how status effects are implemented.
            var statusEffectCount = 0; // Get status effect count from target
            var totalHeal = statusEffectCount * HealPerStatusEffect;
            // Heal player
            GD.Print($"Healed for {totalHeal}.");
        }

        public override bool CanInteractWith(SpellContext context)
        {
            return true;
        }
    }
}
