using System;

namespace Scripts.State
{
    /// <summary>
    /// Represents the different phases of the game following the exact turn loop
    /// </summary>
    public enum GamePhase
    {
        Menu,
        GameStart,
        TurnStart,        // Turn start effects trigger and finish
        CardSelection,    // Player selects cards
        SpellCasting,     // Player input blocked, animations and calculations
        TurnEnd,         // End turn effects trigger and finish
        GameOver,
        Victory
    }

    /// <summary>
    /// Immutable state for the current game phase and related data
    /// </summary>
    public class GamePhaseState
    {
        public GamePhase CurrentPhase { get; }
        public int TurnNumber { get; }
        public float PhaseTimer { get; }
        public bool CanPlayerAct { get; }
        public string PhaseDescription { get; }

        public GamePhaseState(
            GamePhase currentPhase = GamePhase.Menu,
            int turnNumber = 1,
            float phaseTimer = 0f,
            bool canPlayerAct = true,
            string phaseDescription = "")
        {
            CurrentPhase = currentPhase;
            TurnNumber = Math.Max(1, turnNumber);
            PhaseTimer = Math.Max(0f, phaseTimer);
            CanPlayerAct = canPlayerAct;
            PhaseDescription = phaseDescription ?? GetDefaultPhaseDescription(currentPhase);
        }

        /// <summary>
        /// Creates a new GamePhaseState with a different phase
        /// </summary>
        public GamePhaseState WithPhase(GamePhase newPhase)
        {
            return new GamePhaseState(
                newPhase,
                TurnNumber,
                0f, // Reset timer when changing phases
                GetDefaultCanPlayerAct(newPhase),
                GetDefaultPhaseDescription(newPhase));
        }

        /// <summary>
        /// Creates a new GamePhaseState with updated timer
        /// </summary>
        public GamePhaseState WithTimer(float newTimer)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, newTimer, CanPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with incremented turn
        /// </summary>
        public GamePhaseState WithNextTurn()
        {
            return new GamePhaseState(CurrentPhase, TurnNumber + 1, PhaseTimer, CanPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with updated player action status
        /// </summary>
        public GamePhaseState WithPlayerActionStatus(bool canPlayerAct)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, PhaseTimer, canPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with custom description
        /// </summary>
        public GamePhaseState WithDescription(string description)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, PhaseTimer, CanPlayerAct, description);
        }

        /// <summary>
        /// Checks if the current phase allows card selection (only during CardSelection phase)
        /// </summary>
        public bool AllowsCardSelection => CurrentPhase == GamePhase.CardSelection && CanPlayerAct;

        /// <summary>
        /// Checks if the game is in an active gameplay phase
        /// </summary>
        public bool IsInGameplay => CurrentPhase != GamePhase.Menu && 
                                   CurrentPhase != GamePhase.GameStart && 
                                   CurrentPhase != GamePhase.GameOver && 
                                   CurrentPhase != GamePhase.Victory;

        /// <summary>
        /// Checks if the game has ended
        /// </summary>
        public bool IsGameEnded => CurrentPhase == GamePhase.GameOver || CurrentPhase == GamePhase.Victory;

        /// <summary>
        /// Gets the next logical phase based on current phase following the turn loop:
        /// TurnStart -> CardSelection -> SpellCasting -> TurnEnd -> TurnStart (next turn)
        /// Discard loops back to CardSelection
        /// </summary>
        public GamePhase GetNextPhase()
        {
            return CurrentPhase switch
            {
                GamePhase.Menu => GamePhase.GameStart,
                GamePhase.GameStart => GamePhase.TurnStart,
                GamePhase.TurnStart => GamePhase.CardSelection,
                GamePhase.CardSelection => GamePhase.SpellCasting, // When playing cards
                GamePhase.SpellCasting => GamePhase.TurnEnd,
                GamePhase.TurnEnd => GamePhase.TurnStart, // Next turn
                GamePhase.GameOver => GamePhase.Menu,
                GamePhase.Victory => GamePhase.Menu,
                _ => CurrentPhase
            };
        }

        /// <summary>
        /// Gets the phase to go to when discarding (loops back to CardSelection)
        /// </summary>
        public GamePhase GetDiscardPhase()
        {
            return GamePhase.CardSelection;
        }

        private static string GetDefaultPhaseDescription(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Menu => "Main Menu",
                GamePhase.GameStart => "Starting Game...",
                GamePhase.TurnStart => "Turn starting...",
                GamePhase.CardSelection => "Select cards for your spell",
                GamePhase.SpellCasting => "Casting spell...",
                GamePhase.TurnEnd => "Turn ending...",
                GamePhase.GameOver => "Game Over",
                GamePhase.Victory => "Victory!",
                _ => "Unknown Phase"
            };
        }

        private static bool GetDefaultCanPlayerAct(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.CardSelection => true,    // Player can select cards, play, or discard
                GamePhase.SpellCasting => false,    // Player input blocked during animations
                GamePhase.TurnStart => false,       // Automatic turn start effects
                GamePhase.TurnEnd => false,         // Automatic turn end effects
                GamePhase.GameOver => false,
                GamePhase.Victory => false,
                _ => true
            };
        }

        /// <summary>
        /// Validates that the phase state is consistent
        /// </summary>
        public bool IsValid()
        {
            return TurnNumber >= 1 && 
                   PhaseTimer >= 0f &&
                   !string.IsNullOrEmpty(PhaseDescription);
        }

        public override bool Equals(object obj)
        {
            if (obj is GamePhaseState other)
            {
                return CurrentPhase == other.CurrentPhase &&
                       TurnNumber == other.TurnNumber &&
                       Math.Abs(PhaseTimer - other.PhaseTimer) < 0.001f &&
                       CanPlayerAct == other.CanPlayerAct &&
                       PhaseDescription == other.PhaseDescription;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CurrentPhase, TurnNumber, PhaseTimer, CanPlayerAct, PhaseDescription);
        }
    }
}