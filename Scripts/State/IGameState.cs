namespace Scripts.State
{
    /// <summary>
    /// Interface for the single source of truth game state.
    /// All game state should be accessible through this interface.
    /// </summary>
    public interface IGameStateData
    {
        /// <summary>
        /// Global immutable state of all cards across containers (Hand/Played/Discarded)
        /// </summary>
        CardsState Cards { get; }

        /// <summary>
        /// Player hand settings (size/lock). Does not contain the cards themselves.
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
        /// Creates a new game state with updated cards state
        /// </summary>
        IGameStateData WithCards(CardsState newCardsState);

        /// <summary>
        /// Creates a new game state with updated hand settings
        /// </summary>
        IGameStateData WithHand(HandState newHandState);

        /// <summary>
        /// Creates a new game state with updated player state
        /// </summary>
        IGameStateData WithPlayer(PlayerState newPlayerState);

        /// <summary>
        /// Creates a new game state with updated phase state
        /// </summary>
        IGameStateData WithPhase(GamePhaseState newPhaseState);

        /// <summary>
        /// Validates that the current state is consistent and valid
        /// </summary>
        bool IsValid();

        /// <summary>
        /// Gets a string representation of the current state for debugging
        /// </summary>
        string ToString();
    }
}