
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public abstract partial class SpellEffectResource : Resource
    {
        public abstract void Apply(SpellContext context);
    }
}
