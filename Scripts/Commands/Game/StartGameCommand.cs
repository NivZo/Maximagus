using System;
using Godot;
using Scripts.State;

namespace Scripts.Commands.Game
{
    public class StartGameCommand : GameCommand
    {
        public override string GetDescription() => "Start Game";

        public override bool CanExecute()
        {
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.Menu;
        }

        public override CommandResult ExecuteWithResult()
        {
            var currentState = _commandProcessor.CurrentState;
            GD.Print("[StartGameCommand] Starting game...");
            
            // Transition to game start phase
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.GameStart);
            var newState = currentState.WithPhase(newPhaseState);

            // Queue TurnStartCommand as follow-up (no need for QueuedActionsManager)
            var followUpCommands = new[] { new TurnStartCommand() };

            return CommandResult.Success(newState, followUpCommands);
        }
    }
}