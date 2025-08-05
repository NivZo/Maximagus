using System;

namespace Scripts.State
{
    /// <summary>
    /// Represents the different phases of the game
    /// </summary>
    public enum GamePhase
    {
        Menu,
        GameStart,
        CardSelection,
        SpellCasting,
        SpellResolution,
        TurnEnd,
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
        /// Checks if the current phase allows card selection
        /// </summary>
        public bool AllowsCardSelection => CurrentPhase == GamePhase.CardSelection && CanPlayerAct;

        /// <summary>
        /// Checks if the current phase allows spell casting
        /// </summary>
        public bool AllowsSpellCasting => CurrentPhase == GamePhase.CardSelection && CanPlayerAct;

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
        /// Gets the next logical phase based on current phase
        /// </summary>
        public GamePhase GetNextPhase()
        {
            return CurrentPhase switch
            {
                GamePhase.Menu => GamePhase.GameStart,
                GamePhase.GameStart => GamePhase.CardSelection,
                GamePhase.CardSelection => GamePhase.SpellCasting,
                GamePhase.SpellCasting => GamePhase.SpellResolution,
                GamePhase.SpellResolution => GamePhase.TurnEnd,
                GamePhase.TurnEnd => GamePhase.CardSelection,
                GamePhase.GameOver => GamePhase.Menu,
                GamePhase.Victory => GamePhase.Menu,
                _ => CurrentPhase
            };
        }

        private static string GetDefaultPhaseDescription(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Menu => "Main Menu",
                GamePhase.GameStart => "Starting Game...",
                GamePhase.CardSelection => "Select cards for your spell",
                GamePhase.SpellCasting => "Cast your spell",
                GamePhase.SpellResolution => "Resolving spell effects...",
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
                GamePhase.CardSelection => true,
                GamePhase.SpellCasting => false,
                GamePhase.SpellResolution => false,
                GamePhase.TurnEnd => false,
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