
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Resources.Definitions
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
