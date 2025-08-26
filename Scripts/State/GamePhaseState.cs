using System;

namespace Scripts.State
{

    public enum GamePhase
    {
        GameStart,
        TurnStart,        // Turn start effects trigger and finish
        CardSelection,    // Player selects cards
        SpellCasting,     // Player input blocked, animations and calculations
        TurnEnd,         // End turn effects trigger and finish
        GameOver,
        Victory
    }

    public class GamePhaseState
    {
        public GamePhase CurrentPhase { get; }
        public int TurnNumber { get; }
        public string PhaseDescription { get; }

        public GamePhaseState(
            GamePhase currentPhase = GamePhase.GameStart,
            int turnNumber = 1,
            string phaseDescription = "")
        {
            CurrentPhase = currentPhase;
            TurnNumber = Math.Max(1, turnNumber);
            PhaseDescription = phaseDescription ?? GetDefaultPhaseDescription(currentPhase);
        }

        public GamePhaseState WithPhase(GamePhase newPhase)
        {
            return new GamePhaseState(
                newPhase,
                TurnNumber,
                GetDefaultPhaseDescription(newPhase));
        }

        public GamePhaseState WithNextTurn()
        {
            return new GamePhaseState(CurrentPhase, TurnNumber + 1, PhaseDescription);
        }

        public GamePhaseState WithDescription(string description)
        {
            return new GamePhaseState(CurrentPhase, TurnNumber, description);
        }

        public bool AllowsCardSelection => CurrentPhase == GamePhase.CardSelection;

        public bool IsInGameplay => CurrentPhase != GamePhase.GameStart && 
                                   CurrentPhase != GamePhase.GameOver && 
                                   CurrentPhase != GamePhase.Victory;

        public bool IsGameEnded => CurrentPhase == GamePhase.GameOver || CurrentPhase == GamePhase.Victory;

        private static string GetDefaultPhaseDescription(GamePhase phase)
        {
            return phase switch
            {
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

        public bool IsValid()
        {
            return TurnNumber >= 1 && 
                   !string.IsNullOrEmpty(PhaseDescription);
        }

        public override bool Equals(object obj)
        {
            if (obj is GamePhaseState other)
            {
                return CurrentPhase == other.CurrentPhase &&
                       TurnNumber == other.TurnNumber &&
                       PhaseDescription == other.PhaseDescription;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CurrentPhase, TurnNumber, PhaseDescription);
        }
    }
}