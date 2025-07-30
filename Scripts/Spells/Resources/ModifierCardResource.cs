
using Godot;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public abstract partial class ModifierCardResource : SpellCardResource
    {
        public ModifierCardResource()
        {
            CardType = CardType.Modifier;
        }
    }
}
