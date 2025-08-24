using Godot;
using Godot.Collections;
using Maximagus.Resources.Definitions.Actions;
using Scripts.Commands;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<GameCommand> CreateExecutionCommands(string cardId)
        {
            return Actions.Select(action => action.CreateExecutionCommand(cardId));
        }
    }
}