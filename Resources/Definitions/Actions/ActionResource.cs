
using System;
using Godot;
using Scripts.State;
using Scripts.Commands;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public abstract partial class ActionResource : Resource
    {

        public string ActionId { get; private set; }

        public ActionResource()
        {
            ResourceLocalToScene = true;
            ActionId = Resource.GenerateSceneUniqueId();
        }

        public abstract Color PopUpEffectColor { get; }
        public abstract string GetPopUpEffectText(IGameStateData gameState);
        public abstract GameCommand CreateExecutionCommand(string cardId);
    }
}
