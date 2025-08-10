using System;

namespace Scripts.State
{
    /// <summary>
    /// Immutable settings for the player's hand (size/lock).
    /// Cards live in CardsState and are not kept here.
    /// </summary>
    public class HandState
    {
        public int MaxHandSize { get; }
        public bool IsLocked { get; }

        public HandState(int maxHandSize = 10, bool isLocked = false)
        {
            MaxHandSize = maxHandSize;
            IsLocked = isLocked;
        }

        /// <summary>
        /// Creates a new HandState with updated lock status
        /// </summary>
        public HandState WithLockStatus(bool isLocked)
        {
            return new HandState(MaxHandSize, isLocked);
        }

        /// <summary>
        /// Validates hand settings only (card constraints are validated in CardsState)
        /// </summary>
        public bool IsValid()
        {
            return MaxHandSize >= 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is HandState other)
            {
                return MaxHandSize == other.MaxHandSize &&
                       IsLocked == other.IsLocked;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MaxHandSize, IsLocked);
        }
    }
}