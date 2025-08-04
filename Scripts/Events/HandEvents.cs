using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Events
{
    public class HandSubmittedEvent
    {
        public SpellCardResource[] Cards { get; set; }
        public HandActionType ActionType { get; set; }
    }

    public class HandLimitReachedEvent
    {
        public int RemainingHands { get; set; }
        public int RemainingDiscards { get; set; }
    }

    public class CardsRedrawEvent
    {
        public SpellCardResource[] NewCards { get; set; }
    }
}
