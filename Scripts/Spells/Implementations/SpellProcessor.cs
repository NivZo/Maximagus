
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessor : Node
    {
        public void ProcessSpell(Array<SpellCardResource> cards)
        {
            GD.Print("--- Processing Spell ---");
            var context = new SpellContext();


            GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.CardName))}");

            foreach (var card in cards)
            {
                GD.Print($"- Executing card: {card.CardName}");
                card.Execute(context);
            }

            GD.Print($"Spell total damage dealt: {context.TotalDamageDealt}");
            GD.Print("--- Spell Finished ---");
        }
    }
}
