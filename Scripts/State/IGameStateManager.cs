using Scripts.State;

namespace Scripts.State
{
    /// <summary>
    /// Interface for managing the game state as a single source of truth
    /// </summary>
    public interface IGameStateManager
    {
        /// <summary>
        /// Gets the current game state
        /// </summary>
        IGameStateData CurrentState { get; }

        /// <summary>
        /// Updates the game state
        /// </summary>
        void UpdateState(IGameStateData newState);

        /// <summary>
        /// Syncs the GameState with the real game objects
        /// </summary>
        void SyncWithRealGame();

        /// <summary>
        /// Event fired when the game state changes
        /// </summary>
        event System.Action<IGameStateData, IGameStateData> StateChanged;
    }
}