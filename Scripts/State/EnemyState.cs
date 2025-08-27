using System;

namespace Scripts.State
{

    public class EnemyState
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; }

        public EnemyState(int maxHealth = 100, int currentHealth = 100)
        {
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
        }

        public EnemyState WithMaxHealth(int maxHealth)
        {
            return new EnemyState(maxHealth, CurrentHealth);
        }

        public EnemyState WithCurrentHealth(int currentHealth)
        {
            return new EnemyState(MaxHealth, currentHealth);
        }

        public bool IsValid()
        {
            return MaxHealth >= 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is EnemyState other)
            {
                return MaxHealth == other.MaxHealth &&
                    CurrentHealth == other.CurrentHealth;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MaxHealth, CurrentHealth);
        }
    }
}