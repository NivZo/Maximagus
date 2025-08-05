using Scripts.State;

namespace Scripts.State
{
    /// <summary>
    /// Interface for objects that need to be notified when the game state changes
    /// </summary>
    public interface IGameStateObserver
    {
        /// <summary>
        /// Called when the game state changes
        /// </summary>
        /// <param name="previousState">The previous game state</param>
        /// <param name="newState">The new game state</param>
        void OnGameStateChanged(IGameStateData previousState, IGameStateData newState);
    }
}