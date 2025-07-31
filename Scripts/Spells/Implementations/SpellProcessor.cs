
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessor : Node
    {
        public SpellResult ProcessSpell(Array<SpellCardResource> cards)
        {
            GD.Print("--- Processing Spell ---");
            var context = new SpellContext();


            GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.CardName))}");

            foreach (var card in cards)
            {
                GD.Print($"- Executing card: {card.CardName}");
                card.Execute(context);
            }

            // Apply queued effects
            GD.Print("--- Spell Finished ---");

            var result = new SpellResult();
            return result;
        }
    }

    public partial class SpellResult : RefCounted
    {
        // This class will hold the results of the spell.
    }
}
