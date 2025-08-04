using Godot;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Scripts.Models
{
    public partial class Card : Node
    {
        public SpellCardResource Resource { get; private set; }

        public void SetupCardResource(SpellCardResource resource)
        {
            Resource = resource;
        }
    }
}
