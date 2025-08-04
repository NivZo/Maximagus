using System.Collections.Generic;
using Scripts.Commands;
using Scripts.Commands.Card;
using Scripts.Commands.Hand;
using Scripts.State;

namespace Scripts.Validation
{
    /// <summary>
    /// Validates commands based on current game phase
    /// </summary>
    public class GamePhaseValidationRule : ICommandValidationRule
    {
        public ValidationRuleResult Validate(IGameCommand command, IGameStateData currentState)
        {
            var errors = new List<string>();

            // Check if player can act in current phase
            if (!currentState.Phase.CanPlayerAct)
            {
                errors.Add($"Player cannot act during {currentState.Phase.CurrentPhase} phase");
                return ValidationRuleResult.Invalid(errors.AsReadOnly());
            }

            // Phase-specific command validation
            switch (currentState.Phase.CurrentPhase)
            {
                case GamePhase.CardSelection:
                    if (command is PlayHandCommand)
                        errors.Add("Cannot play cards during card selection phase");
                    break;

                case GamePhase.SpellCasting:
                    // All commands allowed during spell casting
                    break;

                case GamePhase.SpellResolution:
                case GamePhase.EnemyTurn:
                case GamePhase.TurnEnd:
                    errors.Add($"No player actions allowed during {currentState.Phase.CurrentPhase} phase");
                    break;

                case GamePhase.GameOver:
                case GamePhase.Victory:
                    errors.Add("Game has ended - no further actions allowed");
                    break;
            }

            return errors.Count > 0 
                ? ValidationRuleResult.Invalid(errors.AsReadOnly()) 
                : ValidationRuleResult.Valid();
        }
    }

    /// <summary>
    /// Validates commands based on player state
    /// </summary>
    public class PlayerStateValidationRule : ICommandValidationRule
    {
        public ValidationRuleResult Validate(IGameCommand command, IGameStateData currentState)
        {
            var errors = new List<string>();

            // Check if player is alive
            if (!currentState.Player.IsAlive)
            {
                errors.Add("Player is dead - no actions allowed");
                return ValidationRuleResult.Invalid(errors.AsReadOnly());
            }

            // Check hands remaining for play commands
            if (command is PlayHandCommand && !currentState.Player.HasHandsRemaining)
            {
                errors.Add("No hands remaining to play cards");
            }

            return errors.Count > 0 
                ? ValidationRuleResult.Invalid(errors.AsReadOnly()) 
                : ValidationRuleResult.Valid();
        }
    }

    /// <summary>
    /// Validates commands based on hand state
    /// </summary>
    public class HandStateValidationRule : ICommandValidationRule
    {
        public ValidationRuleResult Validate(IGameCommand command, IGameStateData currentState)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check if hand is locked
            if (currentState.Hand.IsLocked)
            {
                errors.Add("Hand is locked - no hand modifications allowed");
                return ValidationRuleResult.Invalid(errors.AsReadOnly());
            }

            // Specific validations for different command types
            switch (command)
            {
                case SelectCardCommand selectCmd:
                    ValidateSelectCard(selectCmd, currentState, errors, warnings);
                    break;

                case DeselectCardCommand deselectCmd:
                    ValidateDeselectCard(deselectCmd, currentState, errors, warnings);
                    break;

                case PlayHandCommand:
                    ValidatePlayHand(currentState, errors, warnings);
                    break;

                case DiscardHandCommand:
                    ValidateDiscardHand(currentState, errors, warnings);
                    break;

                case ReorderCardsCommand reorderCmd:
                    ValidateReorderCards(reorderCmd, currentState, errors, warnings);
                    break;
            }

            if (errors.Count > 0)
                return ValidationRuleResult.Invalid(errors.AsReadOnly());

            return warnings.Count > 0 
                ? ValidationRuleResult.Invalid(warnings.AsReadOnly(), false) 
                : ValidationRuleResult.Valid();
        }

        private void ValidateSelectCard(SelectCardCommand command, IGameStateData state, List<string> errors, List<string> warnings)
        {
            // Maximum selection limit check (example business rule)
            const int maxSelectedCards = 5;
            if (state.Hand.SelectedCount >= maxSelectedCards)
            {
                errors.Add($"Cannot select more than {maxSelectedCards} cards");
            }
        }

        private void ValidateDeselectCard(DeselectCardCommand command, IGameStateData state, List<string> errors, List<string> warnings)
        {
            // No specific validation needed beyond base checks
        }

        private void ValidatePlayHand(IGameStateData state, List<string> errors, List<string> warnings)
        {
            if (state.Hand.SelectedCount == 0)
            {
                errors.Add("No cards selected to play");
            }

            // Minimum cards for spell warning
            const int minRecommendedCards = 2;
            if (state.Hand.SelectedCount < minRecommendedCards)
            {
                warnings.Add($"Playing with less than {minRecommendedCards} cards may be less effective");
            }
        }

        private void ValidateDiscardHand(IGameStateData state, List<string> errors, List<string> warnings)
        {
            if (state.Hand.SelectedCount == 0)
            {
                errors.Add("No cards selected to discard");
            }

            // Warning about discarding many cards
            const int manyCardsThreshold = 3;
            if (state.Hand.SelectedCount >= manyCardsThreshold)
            {
                warnings.Add($"Discarding {state.Hand.SelectedCount} cards - this cannot be undone");
            }
        }

        private void ValidateReorderCards(ReorderCardsCommand command, IGameStateData state, List<string> errors, List<string> warnings)
        {
            // Reordering validation is mostly handled in the command's CanExecute method
            // Could add warnings about reordering selected cards
            if (state.Hand.SelectedCount > 0)
            {
                warnings.Add("Reordering cards while some are selected may affect selection order");
            }
        }
    }
}