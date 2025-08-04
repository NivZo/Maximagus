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
        EnemyTurn,
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
        public bool IsPlayerTurn { get; }
        public bool CanPlayerAct { get; }
        public string PhaseDescription { get; }

        public GamePhaseState(
            GamePhase currentPhase = GamePhase.Menu,
            int turnNumber = 1,
            float phaseTimer = 0f,
            bool isPlayerTurn = true,
            bool canPlayerAct = true,
            string phaseDescription = "")
        {
            CurrentPhase = currentPhase;
            TurnNumber = Math.Max(1, turnNumber);
            PhaseTimer = Math.Max(0f, phaseTimer);
            IsPlayerTurn = isPlayerTurn;
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
                GetDefaultIsPlayerTurn(newPhase),
                GetDefaultCanPlayerAct(newPhase),
                GetDefaultPhaseDescription(newPhase));
        }

        /// <summary>
        /// Creates a new GamePhaseState with updated timer
        /// </summary>
        public GamePhaseState WithTimer(float newTimer)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, newTimer, IsPlayerTurn, CanPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with incremented turn
        /// </summary>
        public GamePhaseState WithNextTurn()
        {
            return new GamePhaseState(CurrentPhase, TurnNumber + 1, PhaseTimer, IsPlayerTurn, CanPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with updated player action status
        /// </summary>
        public GamePhaseState WithPlayerActionStatus(bool canPlayerAct)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, PhaseTimer, IsPlayerTurn, canPlayerAct, PhaseDescription);
        }

        /// <summary>
        /// Creates a new GamePhaseState with custom description
        /// </summary>
        public GamePhaseState WithDescription(string description)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, PhaseTimer, IsPlayerTurn, CanPlayerAct, description);
        }

        /// <summary>
        /// Checks if the current phase allows card selection
        /// </summary>
        public bool AllowsCardSelection => CurrentPhase == GamePhase.CardSelection && CanPlayerAct;

        /// <summary>
        /// Checks if the current phase allows spell casting
        /// </summary>
        public bool AllowsSpellCasting => CurrentPhase == GamePhase.SpellCasting && CanPlayerAct;

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
                GamePhase.SpellResolution => GamePhase.EnemyTurn,
                GamePhase.EnemyTurn => GamePhase.TurnEnd,
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
                GamePhase.EnemyTurn => "Enemy turn",
                GamePhase.TurnEnd => "Turn ending...",
                GamePhase.GameOver => "Game Over",
                GamePhase.Victory => "Victory!",
                _ => "Unknown Phase"
            };
        }

        private static bool GetDefaultIsPlayerTurn(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.CardSelection => true,
                GamePhase.SpellCasting => true,
                GamePhase.EnemyTurn => false,
                _ => true
            };
        }

        private static bool GetDefaultCanPlayerAct(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.CardSelection => true,
                GamePhase.SpellCasting => true,
                GamePhase.SpellResolution => false,
                GamePhase.EnemyTurn => false,
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
                       IsPlayerTurn == other.IsPlayerTurn &&
                       CanPlayerAct == other.CanPlayerAct &&
                       PhaseDescription == other.PhaseDescription;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CurrentPhase, TurnNumber, PhaseTimer, IsPlayerTurn, CanPlayerAct, PhaseDescription);
        }
    }
}