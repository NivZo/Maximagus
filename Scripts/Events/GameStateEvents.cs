using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Events
{
    public class GameStateChangedEvent
    {
        public IGameState PreviousState { get; set; }
        public IGameState NewState { get; set; }
    }
}
