using System;
using Godot;
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
            GD.Print("[StartGameCommand] Execute() called!");
            GD.Print($"[StartGameCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // Start the game by transitioning from Menu to the first gameplay phase
            var nextPhase = currentState.Phase.GetNextPhase();
            GD.Print($"[StartGameCommand] Next phase: {nextPhase}");
            
            var newPhaseState = currentState.Phase.WithPhase(nextPhase);
            var newState = currentState.WithPhase(newPhaseState);
            
            GD.Print($"[StartGameCommand] Execute() completed - new phase: {newState.Phase.CurrentPhase}");
            
            return newState;
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // Starting a game typically can't be undone
            return null;
        }
    }
}