using System;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to start the game (Menu -> TurnStart -> CardSelection)
    /// </summary>
    public class StartGameCommand : IGameCommand
    {
        public string GetDescription() => "Start Game";

        public bool CanExecute(IGameStateData currentState)
        {
            // Only allow starting if we're in Menu phase
            return currentState.Phase.CurrentPhase == GamePhase.Menu;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print("[StartGameCommand] Execute() called!");
            GD.Print($"[StartGameCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // Only transition from Menu to TurnStart (following the turn loop)
            if (currentState.Phase.CurrentPhase != GamePhase.Menu)
            {
                GD.Print("[StartGameCommand] Game already started!");
                return currentState;
            }
            
            // Go to TurnStart phase first (which will draw cards and then transition to CardSelection)
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnStart);
            var newState = currentState.WithPhase(newPhaseState);
            
            GD.Print($"[StartGameCommand] Game started - new phase: {newState.Phase.CurrentPhase}");
            
            return newState;
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // Starting a game typically can't be undone
            return null;
        }
    }
}