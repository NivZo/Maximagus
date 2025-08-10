using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Maximagus.Scripts.Managers;
using Scripts.Commands;
using System;
using System.Threading;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private ILogger _logger;
        private IGameCommandProcessor _commandProcessor;
        private IStatusEffectManager _statusEffectManager;
        private CardsRoot _cardsRoot;

        public SpellProcessingManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
            _cardsRoot = ServiceLocator.GetService<CardsRoot>();
        }

        public void ProcessSpell(Action callback)
        {
            var currentState = _commandProcessor.CurrentState;
            var playedCardStates = currentState.Hand.PlayedCards;

            if (playedCardStates.Count() == 0)
            {
                _logger.LogError("No played cards found in state");
                return;
            }

            var context = new SpellContext();
            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

            foreach (var cardState in playedCardStates)
            {
                var card = _cardsRoot.GetCardById(cardState.CardId);
                card.AnimateScale(1.4f, 1f, Tween.TransitionType.Elastic);
                cardState.Resource.Execute(context);
            }
            
            (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(10).Timeout += callback;
        }
    }
}
