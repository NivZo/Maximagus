using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Events
{
    public class TurnStartedEvent
    {
        public int TurnNumber { get; set; }
    }

    public class TurnEndedEvent
    {
        public int TurnNumber { get; set; }
    }

    public class TurnPhaseChangedEvent
    {
        public TurnPhase PreviousPhase { get; set; }
        public TurnPhase NewPhase { get; set; }
        public int TurnNumber { get; set; }
    }
}
