using System;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to start the game (Menu -> TurnStart, or no-op if already in gameplay)
    /// </summary>
    public class StartGameCommand : IGameCommand
    {
        public string GetDescription() => "Start Game";

        public bool CanExecute(IGameStateData currentState)
        {
            // Allow starting from Menu phase, or if already in TurnStart (game already started)
            return currentState.Phase.CurrentPhase == GamePhase.Menu;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print("[StartGameCommand] Execute() called!");
            
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            queuedActionsManager.QueueAction(() => { ServiceLocator.GetService<IGameCommandProcessor>().ExecuteCommand(new TurnStartCommand()); }, .1f);

            return currentState;
        }
    }
}