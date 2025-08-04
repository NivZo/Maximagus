using Scripts.State;

namespace Scripts.Commands
{
    /// <summary>
    /// Base interface for all game commands that modify game state.
    /// Commands must be immutable and side-effect free.
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>
        /// Determines if this command can be executed in the current state.
        /// </summary>
        /// <param name="currentState">The current game state</param>
        /// <returns>True if the command can be executed, false otherwise</returns>
        bool CanExecute(IGameStateData currentState);

        /// <summary>
        /// Executes the command and returns a new game state.
        /// Must not modify the input state - returns new immutable state.
        /// </summary>
        /// <param name="currentState">The current game state</param>
        /// <returns>New game state with changes applied</returns>
        IGameStateData Execute(IGameStateData currentState);

        /// <summary>
        /// Creates an undo command that can reverse this command's effects.
        /// </summary>
        /// <param name="previousState">The state before this command was executed</param>
        /// <returns>Command that can undo this command's effects</returns>
        IGameCommand CreateUndoCommand(IGameStateData previousState);

        /// <summary>
        /// Gets a human-readable description of this command for debugging.
        /// </summary>
        /// <returns>Description of the command</returns>
        string GetDescription();
    }
}