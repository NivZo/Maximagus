
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public abstract partial class ActionResource : Resource, IAction
    {
        public abstract void Execute(SpellContext context);
    }
}
