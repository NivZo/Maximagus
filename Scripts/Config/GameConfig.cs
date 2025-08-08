using Godot;

namespace Scripts.Config
{
    /// <summary>
    /// Centralized configuration constants for the game.
    /// Replaces magic numbers scattered throughout the codebase.
    /// </summary>
    public static class GameConfig
    {
        // Card Interaction Constants
        public const float DRAG_THRESHOLD = 4f;
        public const float SELECTION_VERTICAL_OFFSET = -64.0f;
        
        // Hand Layout Constants
        public const float DEFAULT_CARDS_CURVE_MULTIPLIER = 20.0f;
        public const float DEFAULT_CARDS_ROTATION_MULTIPLIER = 5.0f;
        
        // Game Rules Constants
        public const int DEFAULT_MAX_HAND_SIZE = 10;
        public const int DEFAULT_MAX_HANDS_PER_ENCOUNTER = 5;
        public const int DEFAULT_MAX_DISCARDS_PER_ENCOUNTER = 5;
        public const int DEFAULT_MAX_CARDS_PER_SUBMISSION = 4;
        
        // Performance Constants
        public const int COMMAND_POOL_INITIAL_SIZE = 50;
        public const int LAYOUT_CACHE_MAX_SIZE = 20;
        
        // Visual Constants
        public const float CARD_ANIMATION_SPEED = 10.0f;
        public const float HOVER_SCALE_FACTOR = 1.1f;
        
        // Input Constants
        public const double DOUBLE_CLICK_TIME_THRESHOLD = 0.3; // seconds
        public const float MOUSE_SENSITIVITY = 1.0f;
        
        // Debug Constants
        public const bool ENABLE_STATE_VALIDATION_LOGGING = false;
        public const bool ENABLE_PERFORMANCE_PROFILING = false;
        public const bool ENABLE_COMMAND_HISTORY_DEBUGGING = false;
    }
}