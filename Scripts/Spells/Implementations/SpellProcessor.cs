
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Interfaces;
using Maximagus.Scripts.Spells.Resources;
using System.Linq;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessor : Node
    {
        [Signal]
        public delegate void SpellExecutedEventHandler(SpellResult result);

        [Signal]
        public delegate void CardExecutedEventHandler(SpellCardResource card, SpellContext context);

        public SpellResult ProcessSpell(Array<SpellCardResource> cards)
        {
            GD.Print("--- Processing Spell ---");
            var context = new SpellContext();


            GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.CardName))}");

            foreach (var card in cards)
            {
                if (card.CanInteractWith(context))
                {
                    GD.Print($"- Executing card: {card.CardName}");
                    card.Execute(context);
                    EmitSignal(SignalName.CardExecuted, card, context);
                }
                else
                {
                    GD.Print($"- Skipping card (cannot interact): {card.CardName}");
                }
            }

            // Apply queued effects
            GD.Print("--- Spell Finished ---");

            var result = new SpellResult();
            EmitSignal(SignalName.SpellExecuted, result);
            return result;
        }
    }

    public partial class SpellResult : RefCounted
    {
        // This class will hold the results of the spell.
    }
}
