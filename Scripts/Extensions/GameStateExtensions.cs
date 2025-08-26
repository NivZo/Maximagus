using System;
using Scripts.State;

namespace Scripts.Extensions
{

    public static class GameStateExtensions
    {
        public static EncounterState GetEncounterState(this IGameStateData gameState)
        {
            return EncounterState.FromGameState(gameState, DateTime.UtcNow);
        }
        public static EncounterState GetEncounterState(this IGameStateData gameState, DateTime timestamp)
        {
            return EncounterState.FromGameState(gameState, timestamp);
        }
        public static IGameStateData WithEncounterState(
            this IGameStateData gameState,
            EncounterState encounterState)
        {
            if (encounterState == null)
                throw new ArgumentNullException(nameof(encounterState));

            return encounterState.ApplyToGameState(gameState);
        }
    }
}