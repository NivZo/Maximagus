using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Validation
{
    /// <summary>
    /// Validates game state consistency and business rules
    /// </summary>
    public class StateValidator
    {
        /// <summary>
        /// Validates a game state for consistency and business rule compliance
        /// </summary>
        /// <param name="gameState">The game state to validate</param>
        /// <returns>Validation result with detailed information</returns>
        public StateValidationResult ValidateState(IGameStateData gameState)
        {
            if (gameState == null)
                return StateValidationResult.Invalid("Game state cannot be null");

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate individual components
            ValidateHandState(gameState.Hand, errors, warnings);
            ValidatePlayerState(gameState.Player, errors, warnings);
            ValidatePhaseState(gameState.Phase, errors, warnings);

            // Cross-component validation
            ValidateCrossComponentRules(gameState, errors, warnings);

            if (errors.Count > 0)
                return StateValidationResult.Invalid(errors, warnings);

            if (warnings.Count > 0)
                return StateValidationResult.ValidWithWarnings(warnings);

            return StateValidationResult.Valid();
        }

        private void ValidateHandState(HandState hand, List<string> errors, List<string> warnings)
        {
            // Basic hand validation
            if (!hand.IsValid())
            {
                errors.Add("Hand state is invalid");
                return;
            }

            // Check for duplicate card IDs
            var cardIds = hand.Cards.Select(c => c.CardId).ToList();
            var duplicates = cardIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate card ID found in hand: {duplicate}");
            }

            // Check selected cards consistency
            var selectedCardIds = hand.SelectedCardIds.ToHashSet();
            var actualCardIds = hand.Cards.Select(c => c.CardId).ToHashSet();
            var invalidSelections = selectedCardIds.Except(actualCardIds);
            foreach (var invalidId in invalidSelections)
            {
                errors.Add($"Selected card ID not found in hand: {invalidId}");
            }

            // Business rule validations
            const int maxHandSize = 10;
            if (hand.Count > maxHandSize)
            {
                errors.Add($"Hand size {hand.Count} exceeds maximum of {maxHandSize}");
            }

            const int maxSelectedCards = 5;
            if (hand.SelectedCount > maxSelectedCards)
            {
                warnings.Add($"Selected {hand.SelectedCount} cards, which is more than typical maximum of {maxSelectedCards}");
            }

            // Check for cards in invalid states
            foreach (var card in hand.Cards)
            {
                if (card.IsDragging && card.IsSelected)
                {
                    warnings.Add($"Card {card.CardId} is both dragging and selected - unusual state");
                }

                if (card.Position < 0)
                {
                    errors.Add($"Card {card.CardId} has invalid position: {card.Position}");
                }
            }
        }

        private void ValidatePlayerState(PlayerState player, List<string> errors, List<string> warnings)
        {
            // Basic player validation
            if (!player.IsValid())
            {
                errors.Add("Player state is invalid");
                return;
            }

            // Health validation
            if (player.Health < 0)
            {
                errors.Add($"Player health cannot be negative: {player.Health}");
            }

            if (player.Health > player.MaxHealth)
            {
                errors.Add($"Player health {player.Health} exceeds maximum {player.MaxHealth}");
            }

            // Mana validation
            if (player.Mana < 0)
            {
                errors.Add($"Player mana cannot be negative: {player.Mana}");
            }

            if (player.Mana > player.MaxMana)
            {
                errors.Add($"Player mana {player.Mana} exceeds maximum {player.MaxMana}");
            }

            // Hands validation
            if (player.RemainingHands < 0)
            {
                errors.Add($"Remaining hands cannot be negative: {player.RemainingHands}");
            }

            if (player.RemainingHands > player.MaxHands)
            {
                errors.Add($"Remaining hands {player.RemainingHands} exceeds maximum {player.MaxHands}");
            }

            // Warnings for critical states
            if (player.Health <= player.MaxHealth * 0.2f)
            {
                warnings.Add($"Player health is critically low: {player.Health}/{player.MaxHealth}");
            }

            if (player.RemainingHands == 1)
            {
                warnings.Add("Only 1 hand remaining - choose carefully");
            }
        }

        private void ValidatePhaseState(GamePhaseState phase, List<string> errors, List<string> warnings)
        {
            // Basic phase validation
            if (!phase.IsValid())
            {
                errors.Add("Phase state is invalid");
                return;
            }

            // Turn validation
            if (phase.TurnNumber < 1)
            {
                errors.Add($"Turn number must be positive: {phase.TurnNumber}");
            }

            // Timer validation
            if (phase.PhaseTimer < 0)
            {
                errors.Add($"Phase timer cannot be negative: {phase.PhaseTimer}");
            }

            // Phase consistency checks
            if (phase.CurrentPhase == GamePhase.GameOver || phase.CurrentPhase == GamePhase.Victory)
            {
                if (phase.CanPlayerAct)
                {
                    errors.Add($"Player should not be able to act during {phase.CurrentPhase} phase");
                }
            }

            // Long phase warning
            const float longPhaseThreshold = 300f; // 5 minutes
            if (phase.PhaseTimer > longPhaseThreshold)
            {
                warnings.Add($"Phase has been active for {phase.PhaseTimer:F1} seconds - unusually long");
            }
        }

        private void ValidateCrossComponentRules(IGameStateData gameState, List<string> errors, List<string> warnings)
        {
            // Player death vs game phase
            if (!gameState.Player.IsAlive && gameState.Phase.CurrentPhase != GamePhase.GameOver)
            {
                errors.Add("Player is dead but game phase is not GameOver");
            }

            // Hand selection vs phase
            if (gameState.Hand.SelectedCount > 0 && !gameState.Phase.CanPlayerAct)
            {
                errors.Add("Cards are selected but player cannot act in current phase");
            }

            // Hand lock vs phase
            if (gameState.Hand.IsLocked && gameState.Phase.AllowsCardSelection)
            {
                warnings.Add("Hand is locked during a phase that typically allows card selection");
            }

            // No hands remaining vs phase
            if (!gameState.Player.HasHandsRemaining && gameState.Phase.AllowsSpellCasting)
            {
                errors.Add("No hands remaining but phase allows spell casting");
            }

            // Turn consistency
            if (gameState.Phase.TurnNumber > 100)
            {
                warnings.Add($"Turn number is very high: {gameState.Phase.TurnNumber} - potential infinite loop?");
            }

            // Resource consistency
            var totalCardsPlayed = gameState.Player.MaxHands - gameState.Player.RemainingHands;
            if (totalCardsPlayed < 0)
            {
                errors.Add("Calculated negative cards played - inconsistent player state");
            }
        }
    }

    /// <summary>
    /// Result of state validation
    /// </summary>
    public class StateValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }

        private StateValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public static StateValidationResult Valid() => 
            new StateValidationResult(true, null, null);

        public static StateValidationResult ValidWithWarnings(IReadOnlyList<string> warnings) => 
            new StateValidationResult(true, null, warnings);

        public static StateValidationResult Invalid(string error) => 
            new StateValidationResult(false, new List<string> { error }, null);

        public static StateValidationResult Invalid(IReadOnlyList<string> errors, IReadOnlyList<string> warnings = null) => 
            new StateValidationResult(false, errors, warnings);
    }
}