using System;
using Godot;

namespace Scripts.State
{

    public class EncounterState
    {
        public SpellState Spell { get; }
        public StatusEffectsState StatusEffects { get; }
        public DateTime Timestamp { get; }
        public int ActionIndex { get; }

        public EncounterState(
            SpellState spell,
            StatusEffectsState statusEffects,
            DateTime timestamp,
            int actionIndex = 0)
        {
            Spell = spell ?? throw new ArgumentNullException(nameof(spell));
            StatusEffects = statusEffects ?? throw new ArgumentNullException(nameof(statusEffects));
            Timestamp = timestamp;
            ActionIndex = actionIndex;
        }

        public EncounterState WithSpell(SpellState newSpell)
        {
            if (newSpell == null) throw new ArgumentNullException(nameof(newSpell));
            return new EncounterState(newSpell, StatusEffects, Timestamp, ActionIndex);
        }

        public EncounterState WithStatusEffects(StatusEffectsState newStatusEffects)
        {
            if (newStatusEffects == null) throw new ArgumentNullException(nameof(newStatusEffects));
            return new EncounterState(Spell, newStatusEffects, Timestamp, ActionIndex);
        }

        public EncounterState WithTimestamp(DateTime newTimestamp)
        {
            return new EncounterState(Spell, StatusEffects, newTimestamp, ActionIndex);
        }

        public EncounterState WithActionIndex(int newActionIndex)
        {
            return new EncounterState(Spell, StatusEffects, Timestamp, newActionIndex);
        }

        public EncounterState WithBoth(SpellState newSpell, StatusEffectsState newStatusEffects)
        {
            if (newSpell == null) throw new ArgumentNullException(nameof(newSpell));
            if (newStatusEffects == null) throw new ArgumentNullException(nameof(newStatusEffects));
            return new EncounterState(newSpell, newStatusEffects, Timestamp, ActionIndex);
        }

        public static EncounterState FromGameState(IGameStateData gameState, DateTime timestamp)
        {
            if (gameState == null) throw new ArgumentNullException(nameof(gameState));
            
            return new EncounterState(
                gameState.Spell,
                gameState.StatusEffects,
                timestamp,
                gameState.Spell.CurrentActionIndex);
        }

        public IGameStateData ApplyToGameState(IGameStateData gameState)
        {
            if (gameState == null) throw new ArgumentNullException(nameof(gameState));
            
            return gameState
                .WithSpell(Spell)
                .WithStatusEffects(StatusEffects);
        }

        public bool IsValid()
        {
            try
            {
                // Validate component states
                if (!Spell.IsValid() || !StatusEffects.IsValid())
                    return false;

                // Validate timestamp is reasonable
                if (Timestamp > DateTime.UtcNow.AddMinutes(1))
                    return false;

                // Validate action index consistency - both should be non-negative and reasonably close
                if (ActionIndex < 0 || Spell.CurrentActionIndex < 0)
                    return false;

                // Allow some flexibility in action index consistency during pre-calculation
                // The difference should not be more than 1 (for cases where one is updated before the other)
                var indexDifference = Math.Abs(ActionIndex - Spell.CurrentActionIndex);
                if (indexDifference > 1)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is EncounterState other)
            {
                return Spell.Equals(other.Spell) &&
                       StatusEffects.Equals(other.StatusEffects) &&
                       Timestamp == other.Timestamp &&
                       ActionIndex == other.ActionIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Spell, StatusEffects, Timestamp, ActionIndex);
        }

        public override string ToString()
        {
            return $"EncounterState[ActionIndex: {ActionIndex}, Timestamp: {Timestamp:HH:mm:ss.fff}, " +
                   $"Spell: {Spell}, StatusEffects: {StatusEffects}]";
        }
    }
}