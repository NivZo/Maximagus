using Godot;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.Commands;
using System;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private const float WaitAfterSubmit = 2f;
        private const float CardAnimationDuration = 1f;
        private const float CardAnimationDelay = 2f;
        private const float ClearPlayedHandDelay = 2f;

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

            ExecuteAfter(() =>
            {
                for (int i = 0; i < playedCardStates.Count(); i++)
                {
                    var cardState = playedCardStates.ElementAt(i);
                    var card = _cardsRoot.GetCardById(cardState.CardId);
                    var delay = i * CardAnimationDelay;

                    ExecuteAfter(() => VisualizeCardExecution(card, context), delay);
                }

                ExecuteAfter(callback, CardAnimationDelay * playedCardStates.Count() + ClearPlayedHandDelay);
            }, WaitAfterSubmit);
        }

        private void VisualizeCardExecution(Card card, SpellContext spellContext)
        {
            card.AnimateScale(1.4f, CardAnimationDuration, Tween.TransitionType.Elastic);
            card.Resource.Execute(spellContext);
        }

        private void ExecuteAfter(Action action, float delay)
        {
            var timer = (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(delay);
            timer.Timeout += action;
        }
    }
}
