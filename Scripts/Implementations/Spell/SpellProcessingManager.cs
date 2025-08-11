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
        private const float CardAnimationDelay = 1.5f;
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

        public void ProcessSpell(Action onFinishCallback)
        {
            var currentState = _commandProcessor.CurrentState;
        
            // Materialize and lock execution order by Position
            var playedCardStates = currentState.Cards
                .PlayedCards?
                .OrderBy(c => c.Position)
                .ToArray();
        
            if (playedCardStates == null || playedCardStates.Length == 0)
            {
                _logger.LogError("No played cards found in state");
                return;
            }
        
            var context = new SpellContext();
            _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);
        
            TimerUtils.ExecuteAfter(() =>
            {
                for (int i = 0; i < playedCardStates.Length; i++)
                {
                    var cardState = playedCardStates[i];
                    var card = _cardsRoot.GetCardById(cardState.CardId);
                    var delay = i * CardAnimationDelay;
        
                    TimerUtils.ExecuteAfter(() => VisualizeCardExecution(card, context), delay);
                }
        
                TimerUtils.ExecuteAfter(onFinishCallback, CardAnimationDelay * playedCardStates.Length + ClearPlayedHandDelay);
            }, WaitAfterSubmit);
        }

        private void VisualizeCardExecution(Card card, SpellContext spellContext)
        {
            card.AnimateScale(1.4f, CardAnimationDuration, Tween.TransitionType.Elastic);
            card.Resource.Execute(spellContext);
            EffectPopUp.Create(card, new(0, -card.Size.Y * .6f), "+ 2 Fire");
        }
    }
}
