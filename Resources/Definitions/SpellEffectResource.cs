
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions
{
    [GlobalClass]
    public abstract partial class SpellEffectResource : Resource
    {
        public abstract void Apply(SpellContext context);
    }
}
