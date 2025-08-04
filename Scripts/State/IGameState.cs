namespace Scripts.State
{
    /// <summary>
    /// Interface for the single source of truth game state.
    /// All game state should be accessible through this interface.
    /// </summary>
    public interface IGameStateData
    {
        /// <summary>
        /// Current state of the player's hand
        /// </summary>
        HandState Hand { get; }

        /// <summary>
        /// Current player state (health, mana, etc.)
        /// </summary>
        PlayerState Player { get; }

        /// <summary>
        /// Current phase of the game
        /// </summary>
        GamePhaseState Phase { get; }

        /// <summary>
        /// Creates a new game state with updated hand state
        /// </summary>
        /// <param name="newHandState">The new hand state</param>
        /// <returns>New immutable game state with updated hand</returns>
        IGameStateData WithHand(HandState newHandState);

        /// <summary>
        /// Creates a new game state with updated player state
        /// </summary>
        /// <param name="newPlayerState">The new player state</param>
        /// <returns>New immutable game state with updated player</returns>
        IGameStateData WithPlayer(PlayerState newPlayerState);

        /// <summary>
        /// Creates a new game state with updated phase state
        /// </summary>
        /// <param name="newPhaseState">The new phase state</param>
        /// <returns>New immutable game state with updated phase</returns>
        IGameStateData WithPhase(GamePhaseState newPhaseState);

        /// <summary>
        /// Validates that the current state is consistent and valid
        /// </summary>
        /// <returns>True if state is valid, false otherwise</returns>
        bool IsValid();

        /// <summary>
        /// Gets a string representation of the current state for debugging
        /// </summary>
        /// <returns>String summary of the state</returns>
        string ToString();
    }
}