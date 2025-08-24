using System;
using Scripts.State;

namespace Scripts.Extensions
{
    /// <summary>
    /// Extension methods for IGameStateData to provide convenient EncounterState integration
    /// </summary>
    public static class GameStateExtensions
    {
        /// <summary>
        /// Creates an EncounterState from the current game state with the current timestamp
        /// </summary>
        /// <param name="gameState">The game state to convert</param>
        /// <returns>EncounterState representing the current encounter state</returns>
        public static EncounterState GetEncounterState(this IGameStateData gameState)
        {
            return EncounterState.FromGameState(gameState, DateTime.UtcNow);
        }

        /// <summary>
        /// Creates an EncounterState from the current game state with a specific timestamp
        /// </summary>
        /// <param name="gameState">The game state to convert</param>
        /// <param name="timestamp">The timestamp to use for the EncounterState</param>
        /// <returns>EncounterState representing the encounter state at the specified time</returns>
        public static EncounterState GetEncounterState(this IGameStateData gameState, DateTime timestamp)
        {
            return EncounterState.FromGameState(gameState, timestamp);
        }

        /// <summary>
        /// Applies an EncounterState to the current game state, updating both spell and status effects
        /// </summary>
        /// <param name="gameState">The game state to update</param>
        /// <param name="encounterState">The EncounterState to apply</param>
        /// <returns>New game state with the EncounterState applied</returns>
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