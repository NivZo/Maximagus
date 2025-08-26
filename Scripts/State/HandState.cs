using System;

namespace Scripts.State
{

    public class HandState
    {
        public int MaxHandSize { get; }
        public bool IsLocked { get; }

        public HandState(int maxHandSize = 10, bool isLocked = false)
        {
            MaxHandSize = maxHandSize;
            IsLocked = isLocked;
        }

        public HandState WithLockStatus(bool isLocked)
        {
            return new HandState(MaxHandSize, isLocked);
        }

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