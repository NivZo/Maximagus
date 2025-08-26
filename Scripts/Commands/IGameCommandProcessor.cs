using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands
{

    public interface IGameCommandProcessor
    {
        public IGameStateData CurrentState { get; }

        public event Action<IGameStateData, IGameStateData> StateChanged;
        public bool ExecuteCommand(GameCommand command);

        public void SetState(IGameStateData newState);

        public void NotifyBlockingCommandFinished();
    }
}