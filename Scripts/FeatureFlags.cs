using System;
using Godot;

namespace Scripts
{
    /// <summary>
    /// Feature flags for controlling the gradual migration from old to new architecture.
    /// These flags allow safe switching between old and new systems during development.
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// Controls whether to use the new unified input system or the legacy input handling.
        /// When false, uses existing CardLogic and GameInputManager.
        /// When true, uses new InputToCommandMapper system.
        /// </summary>
        public static bool UseNewInputSystem { get; set; } = false;

        /// <summary>
        /// Controls whether to use the new GameState system or legacy state management.
        /// When false, uses existing distributed state across components.
        /// When true, uses new centralized GameState with command processor.
        /// </summary>
        public static bool UseNewStateSystem { get; set; } = false;

        /// <summary>
        /// Controls whether to use the new Card system architecture or legacy card management.
        /// When false, uses existing Card/CardLogic/CardVisual system.
        /// When true, uses new CardView/CardController system.
        /// </summary>
        public static bool UseNewCardSystem { get; set; } = false;

        /// <summary>
        /// Controls whether to use the new Hand management system.
        /// When false, uses existing Hand/HandManager system.
        /// When true, uses new HandView/HandController system.
        /// </summary>
        public static bool UseNewHandSystem { get; set; } = false;

        /// <summary>
        /// Enable debug logging for feature flag transitions.
        /// Useful for tracking which systems are being used during development.
        /// </summary>
        public static bool EnableFeatureFlagLogging { get; set; } = true;

        /// <summary>
        /// Emergency flag to disable all new systems and revert to legacy.
        /// Use this if new systems cause critical issues.
        /// </summary>
        public static bool ForceUseLegacySystems { get; set; } = false;

        /// <summary>
        /// Gets whether a specific feature should use the new system.
        /// Takes into account the emergency flag and individual feature flags.
        /// </summary>
        /// <param name="feature">The feature to check</param>
        /// <returns>True if should use new system, false for legacy</returns>
        public static bool ShouldUseNewSystem(FeatureFlag feature)
        {
            // Emergency override - always use legacy if forced
            if (ForceUseLegacySystems)
            {
                LogFeatureFlag(feature, false, "Forced legacy due to emergency flag");
                return false;
            }

            bool useNew = feature switch
            {
                FeatureFlag.InputSystem => UseNewInputSystem,
                FeatureFlag.StateSystem => UseNewStateSystem,
                FeatureFlag.CardSystem => UseNewCardSystem,
                FeatureFlag.HandSystem => UseNewHandSystem,
                _ => false
            };

            if (EnableFeatureFlagLogging)
            {
                LogFeatureFlag(feature, useNew, useNew ? "Using new system" : "Using legacy system");
            }

            return useNew;
        }

        /// <summary>
        /// Enables all new systems (for testing complete new architecture)
        /// </summary>
        public static void EnableAllNewSystems()
        {
            UseNewInputSystem = true;
            UseNewStateSystem = true;
            UseNewCardSystem = true;
            UseNewHandSystem = true;
            ForceUseLegacySystems = false;

            if (EnableFeatureFlagLogging)
            {
                GD.Print("[FeatureFlags] All new systems enabled");
            }
        }

        /// <summary>
        /// Disables all new systems (revert to legacy for safety)
        /// </summary>
        public static void EnableAllLegacySystems()
        {
            UseNewInputSystem = false;
            UseNewStateSystem = false;
            UseNewCardSystem = false;
            UseNewHandSystem = false;
            ForceUseLegacySystems = true;

            if (EnableFeatureFlagLogging)
            {
                GD.Print("[FeatureFlags] All systems reverted to legacy");
            }
        }

        /// <summary>
        /// Gets a summary of current feature flag states for debugging
        /// </summary>
        /// <returns>String summary of all feature flags</returns>
        public static string GetFeatureFlagSummary()
        {
            return $"FeatureFlags Summary:\n" +
                   $"  Input System: {(ShouldUseNewSystem(FeatureFlag.InputSystem) ? "NEW" : "LEGACY")}\n" +
                   $"  State System: {(ShouldUseNewSystem(FeatureFlag.StateSystem) ? "NEW" : "LEGACY")}\n" +
                   $"  Card System: {(ShouldUseNewSystem(FeatureFlag.CardSystem) ? "NEW" : "LEGACY")}\n" +
                   $"  Hand System: {(ShouldUseNewSystem(FeatureFlag.HandSystem) ? "NEW" : "LEGACY")}\n" +
                   $"  Emergency Mode: {(ForceUseLegacySystems ? "ACTIVE" : "INACTIVE")}";
        }

        private static void LogFeatureFlag(FeatureFlag feature, bool useNew, string reason)
        {
            var system = useNew ? "NEW" : "LEGACY";
            GD.Print($"[FeatureFlags] {feature}: {system} - {reason}");
        }
    }

    /// <summary>
    /// Enumeration of feature flags for type-safe feature checking
    /// </summary>
    public enum FeatureFlag
    {
        InputSystem,
        StateSystem,
        CardSystem,
        HandSystem
    }
}