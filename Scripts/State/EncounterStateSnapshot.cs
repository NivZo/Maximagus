using System;

namespace Scripts.State
{
    /// <summary>
    /// Immutable snapshot containing a complete EncounterState and its associated action result.
    /// Used for pre-calculation and snapshot-based execution of spell actions.
    /// </summary>
    public class EncounterStateSnapshot
    {
        public string ActionKey { get; }
        public EncounterState ResultingState { get; }
        public ActionExecutionResult ActionResult { get; }
        public DateTime CreatedAt { get; }

        public EncounterStateSnapshot(
            string actionKey,
            EncounterState resultingState,
            ActionExecutionResult actionResult,
            DateTime createdAt)
        {
            ActionKey = actionKey ?? throw new ArgumentNullException(nameof(actionKey));
            ResultingState = resultingState ?? throw new ArgumentNullException(nameof(resultingState));
            ActionResult = actionResult ?? throw new ArgumentNullException(nameof(actionResult));
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Creates a snapshot with the current timestamp
        /// </summary>
        public static EncounterStateSnapshot Create(
            string actionKey,
            EncounterState resultingState,
            ActionExecutionResult actionResult)
        {
            return new EncounterStateSnapshot(actionKey, resultingState, actionResult, DateTime.UtcNow);
        }

        /// <summary>
        /// Validates that the snapshot is consistent and valid
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Action key should not be null or empty
                if (string.IsNullOrEmpty(ActionKey))
                    return false;

                // Resulting state should be valid
                if (!ResultingState.IsValid())
                    return false;

                // Action result should be valid
                if (!ActionResult.IsValid())
                    return false;

                // Created timestamp should be reasonable
                if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
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
            if (obj is EncounterStateSnapshot other)
            {
                return ActionKey == other.ActionKey &&
                       ResultingState.Equals(other.ResultingState) &&
                       ActionResult.Equals(other.ActionResult) &&
                       CreatedAt == other.CreatedAt;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionKey, ResultingState, ActionResult, CreatedAt);
        }

        public override string ToString()
        {
            return $"EncounterStateSnapshot[{ActionKey}, Created: {CreatedAt:HH:mm:ss.fff}, " +
                   $"State: {ResultingState}, Result: {ActionResult}]";
        }
    }
}