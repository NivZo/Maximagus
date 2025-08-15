using Godot;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.Commands;
using System;
using Scripts.State;
using Maximagus.Resources.Definitions.Actions;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellProcessingManager : ISpellProcessingManager
    {
        private const float WaitAfterSubmit = 1f;
        private const float CardAnimationDuration = .75f;
        private const float CardAnimationDelay = 1f;
        private const float ClearPlayedHandDelay = 1f;

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
        


            TimerUtils.ExecuteAfter(() =>
            {
                var context = new SpellContext();
                _statusEffectManager.TriggerEffects(StatusEffectTrigger.OnSpellCast);

                var actions = playedCardStates
                    .SelectMany<CardState, (Card Card, ActionResource Action)>(state => state.Resource.Actions.Select(action => (_cardsRoot.GetCardById(state.CardId), action)))
                    .ToArray();

                var cardPositions = actions.DistinctBy(action => action.Card).ToDictionary(action => action.Card, action => action.Card.GetCenter() - new Vector2(0, .8f * action.Card.Size.Y));

                for (int i = 0; i < actions.Length; i++)
                {
                    var (card, action) = actions[i];
                    var delay = i * CardAnimationDelay;

                    TimerUtils.ExecuteAfter(() => VisualizeCardExecution(card, action, cardPositions[card], context), delay);
                }
        
                TimerUtils.ExecuteAfter(onFinishCallback, CardAnimationDelay * actions.Length + ClearPlayedHandDelay);
            }, WaitAfterSubmit);
        }

        private void VisualizeCardExecution(Card card, ActionResource action, Vector2 position, SpellContext spellContext)
        {
            card.AnimateScale(1.4f, CardAnimationDuration, Tween.TransitionType.Elastic);
            EffectPopUp.Create(position, action.GetPopUpEffectText(spellContext) , action.PopUpEffectColor);
            card.Resource.Execute(spellContext);
        }
    }
}
