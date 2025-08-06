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

        public override IGameStateData Execute()
        {
            GD.Print("[StartGameCommand] Execute() called!");
            
            var queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
            queuedActionsManager.QueueAction(() => { ServiceLocator.GetService<IGameCommandProcessor>().ExecuteCommand(new TurnStartCommand()); }, .1f);

            return _commandProcessor.CurrentState;
        }
    }
}