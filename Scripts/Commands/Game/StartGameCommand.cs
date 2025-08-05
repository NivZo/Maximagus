using System;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to start the game (Menu -> CardSelection directly, since cards are already created)
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
            
            // Only transition from Menu to CardSelection (since cards are already created in Hand)
            if (currentState.Phase.CurrentPhase != GamePhase.Menu)
            {
                GD.Print("[StartGameCommand] Game already started!");
                return currentState;
            }
            
            // Go directly to CardSelection phase since cards are already created
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
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