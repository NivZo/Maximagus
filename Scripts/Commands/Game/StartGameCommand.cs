using Scripts.State;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to start the game or advance to the next phase
    /// </summary>
    public class StartGameCommand : IGameCommand
    {
        public string GetDescription() => "Start Game";

        public bool CanExecute(IGameStateData currentState)
        {
            // For now, always allow starting the game
            // This could be enhanced to check specific conditions
            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            // Start the game by transitioning from Menu to the first gameplay phase
            var nextPhase = currentState.Phase.GetNextPhase();
            var newPhaseState = currentState.Phase.WithPhase(nextPhase);
            
            return currentState.WithPhase(newPhaseState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // Starting a game typically can't be undone
            return null;
        }
    }
}