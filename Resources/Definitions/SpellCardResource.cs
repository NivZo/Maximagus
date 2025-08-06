using Godot;
using Godot.Collections;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Abstractions
{
    [GlobalClass]
    public partial class SpellCardResource : Resource
    {
        [Export] public string CardResourceId { get; set; }
        [Export] public string CardName { get; set; }
        [Export(PropertyHint.MultilineText)] public string CardDescription { get; set; }
        [Export] public Texture2D CardArt { get; set; }

        [Export] public Array<ActionResource> Actions { get; set; }

        public void Execute(SpellContext context)
        {
            foreach (var action in Actions)
            {
                action.Execute(context);
            }
        }
    }
}