
using Godot;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public abstract partial class ActionCardResource : SpellCardResource
    {
        public ActionCardResource()
        {
            CardType = CardType.Action;
        }
    }
}
