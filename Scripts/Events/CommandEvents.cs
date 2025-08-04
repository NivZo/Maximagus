using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Events
{
    public class PlayCardsRequestedEvent
    {
        public Array<Card> SelectedCards { get; set; }
        public Array<Card> CurrentHandCards { get; set; }
    }

    public class DiscardCardsRequestedEvent
    {
        public Array<Card> SelectedCards { get; set; }
        public Array<Card> CurrentHandCards { get; set; }
    }
}