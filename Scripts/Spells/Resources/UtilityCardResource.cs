
using Godot;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public abstract partial class UtilityCardResource : SpellCardResource
    {
        public UtilityCardResource()
        {
            CardType = CardType.Utility;
        }
    }
}
