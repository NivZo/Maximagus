using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;

namespace Maximagus.Scripts.Spells.Core
{
    public partial class SpellProcessor : Node
    {
        [Signal]
        public delegate void SpellExecutedEventHandler(SpellResult result);

        [Signal]
        public delegate void CardExecutedEventHandler(SpellCardResource card, SpellContext context);

        public SpellResult ProcessSpell(Array<SpellCardResource> cards, ISpellTarget target)
        {
            GD.Print("--- Processing Spell ---");
            var context = new SpellContext { Target = target };

            var sortedCards = cards.OrderBy(c => c.CardType).ThenBy(c => c.GetExecutionPriority()).ToList();

            GD.Print($"Executing {sortedCards.Count()} cards in the following order: {string.Join(", ", sortedCards.Select(c => c.CardName))}");

            foreach (var card in sortedCards)
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