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
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.GameStart;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            _logger.LogInfo("[StartGameCommand] Starting game...");
            
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnStart);
            var newState = currentState.WithPhase(newPhaseState);

            var followUpCommands = new[] { new TurnStartCommand() };

            token.Complete(CommandResult.Success(newState, followUpCommands));
        }
    }
}