using System;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to start the game (Menu -> CardSelection, or no-op if already in gameplay)
    /// </summary>
    public class StartGameCommand : IGameCommand
    {
        public string GetDescription() => "Start Game";

        public bool CanExecute(IGameStateData currentState)
        {
            // Allow starting from Menu phase, or if already in CardSelection (game already started)
            return currentState.Phase.CurrentPhase == GamePhase.Menu || 
                   currentState.Phase.CurrentPhase == GamePhase.CardSelection;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print("[StartGameCommand] Execute() called!");
            GD.Print($"[StartGameCommand] Current phase: {currentState.Phase.CurrentPhase}");
            
            // If we're in Menu, transition to CardSelection
            if (currentState.Phase.CurrentPhase == GamePhase.Menu)
            {
                var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
                var newState = currentState.WithPhase(newPhaseState);
                
                GD.Print("[StartGameCommand] Game started - transitioned to CardSelection phase");
                return newState;
            }
            
            // If we're already in CardSelection, the game is already started - no change needed
            if (currentState.Phase.CurrentPhase == GamePhase.CardSelection)
            {
                GD.Print("[StartGameCommand] Game already started and in CardSelection phase");
                return currentState; // No state change needed
            }
            
            // For other phases, no action (shouldn't happen due to CanExecute check)
            GD.Print("[StartGameCommand] No action needed for current phase");
            return currentState;
        }
    }
}