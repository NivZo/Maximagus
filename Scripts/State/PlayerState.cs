using System;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state for player statistics and resources
    /// </summary>
    public class PlayerState
    {
        public int Health { get; }
        public int MaxHealth { get; }
        public int Mana { get; }
        public int MaxMana { get; }
        public int RemainingHands { get; }
        public int MaxHands { get; }
        public int Score { get; }
        public int Level { get; }

        public PlayerState(
            int health = 100,
            int maxHealth = 100,
            int mana = 10,
            int maxMana = 10,
            int remainingHands = 3,
            int maxHands = 3,
            int score = 0,
            int level = 1)
        {
            Health = Math.Max(0, health);
            MaxHealth = Math.Max(1, maxHealth);
            Mana = Math.Max(0, mana);
            MaxMana = Math.Max(0, maxMana);
            RemainingHands = Math.Max(0, remainingHands);
            MaxHands = Math.Max(1, maxHands);
            Score = Math.Max(0, score);
            Level = Math.Max(1, level);
        }

        /// <summary>
        /// Gets the health as a percentage (0.0 to 1.0)
        /// </summary>
        public float HealthPercentage => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;

        /// <summary>
        /// Gets the mana as a percentage (0.0 to 1.0)
        /// </summary>
        public float ManaPercentage => MaxMana > 0 ? (float)Mana / MaxMana : 0f;

        /// <summary>
        /// Checks if the player is alive
        /// </summary>
        public bool IsAlive => Health > 0;

        /// <summary>
        /// Checks if the player has any hands remaining
        /// </summary>
        public bool HasHandsRemaining => RemainingHands > 0;

        /// <summary>
        /// Creates a new PlayerState with modified health
        /// </summary>
        public PlayerState WithHealth(int newHealth)
        {
            return new PlayerState(newHealth, MaxHealth, Mana, MaxMana, RemainingHands, MaxHands, Score, Level);
        }

        /// <summary>
        /// Creates a new PlayerState with health damage applied
        /// </summary>
        public PlayerState WithDamage(int damage)
        {
            return WithHealth(Health - damage);
        }

        /// <summary>
        /// Creates a new PlayerState with health healing applied
        /// </summary>
        public PlayerState WithHealing(int healing)
        {
            return WithHealth(Math.Min(MaxHealth, Health + healing));
        }

        /// <summary>
        /// Creates a new PlayerState with modified mana
        /// </summary>
        public PlayerState WithMana(int newMana)
        {
            return new PlayerState(Health, MaxHealth, newMana, MaxMana, RemainingHands, MaxHands, Score, Level);
        }

        /// <summary>
        /// Creates a new PlayerState with mana cost applied
        /// </summary>
        public PlayerState WithManaCost(int cost)
        {
            return WithMana(Math.Max(0, Mana - cost));
        }

        /// <summary>
        /// Creates a new PlayerState with mana restoration
        /// </summary>
        public PlayerState WithManaRestore(int restore)
        {
            return WithMana(Math.Min(MaxMana, Mana + restore));
        }

        /// <summary>
        /// Creates a new PlayerState with one less hand remaining
        /// </summary>
        public PlayerState WithHandUsed()
        {
            return new PlayerState(Health, MaxHealth, Mana, MaxMana, RemainingHands - 1, MaxHands, Score, Level);
        }

        /// <summary>
        /// Creates a new PlayerState with modified remaining hands
        /// </summary>
        public PlayerState WithRemainingHands(int newRemainingHands)
        {
            return new PlayerState(Health, MaxHealth, Mana, MaxMana, newRemainingHands, MaxHands, Score, Level);
        }

        /// <summary>
        /// Creates a new PlayerState with added score
        /// </summary>
        public PlayerState WithAddedScore(int points)
        {
            return new PlayerState(Health, MaxHealth, Mana, MaxMana, RemainingHands, MaxHands, Score + points, Level);
        }

        /// <summary>
        /// Creates a new PlayerState with level up
        /// </summary>
        public PlayerState WithLevelUp()
        {
            return new PlayerState(Health, MaxHealth, Mana, MaxMana, RemainingHands, MaxHands, Score, Level + 1);
        }

        /// <summary>
        /// Creates a new PlayerState with updated max stats
        /// </summary>
        public PlayerState WithMaxStats(int newMaxHealth, int newMaxMana, int newMaxHands)
        {
            return new PlayerState(
                Math.Min(Health, newMaxHealth), // Adjust current health if max decreased
                newMaxHealth,
                Math.Min(Mana, newMaxMana), // Adjust current mana if max decreased
                newMaxMana,
                Math.Min(RemainingHands, newMaxHands), // Adjust remaining hands if max decreased
                newMaxHands,
                Score,
                Level);
        }

        /// <summary>
        /// Validates that the player state is consistent
        /// </summary>
        public bool IsValid()
        {
            return Health >= 0 && Health <= MaxHealth &&
                   Mana >= 0 && Mana <= MaxMana &&
                   RemainingHands >= 0 && RemainingHands <= MaxHands &&
                   MaxHealth > 0 && MaxMana >= 0 && MaxHands > 0 &&
                   Score >= 0 && Level >= 1;
        }

        public override bool Equals(object obj)
        {
            if (obj is PlayerState other)
            {
                return Health == other.Health &&
                       MaxHealth == other.MaxHealth &&
                       Mana == other.Mana &&
                       MaxMana == other.MaxMana &&
                       RemainingHands == other.RemainingHands &&
                       MaxHands == other.MaxHands &&
                       Score == other.Score &&
                       Level == other.Level;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Health, MaxHealth, Mana, MaxMana, RemainingHands, MaxHands, Score, Level);
        }
    }
}