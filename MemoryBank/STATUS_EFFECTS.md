# Status Effects

The status effect system is responsible for managing temporary and persistent effects on game entities through centralized state management.

- **`StatusEffectResource`**: A Godot `Resource` that defines a status effect, including its type, trigger condition, decay mode, and behavior.
- **`StatusEffectInstanceData`**: An immutable data structure representing a status effect instance in the centralized game state. It tracks the current number of stacks and application timestamp.
- **`StatusEffectsState`**: A centralized state object that manages all active status effects as part of the main GameState. Provides immutable operations for adding, removing, and querying effects.
- **`StatusEffectLogicManager`**: A static manager class containing pure functions for status effect operations. Handles effect application, triggering, decay processing, and stack queries.
- **Status Effect Commands**: Commands like `ApplyStatusEffectCommand`, `TriggerStatusEffectsCommand`, and `ProcessStatusEffectDecayCommand` that handle status effect operations through the command system.
- **Triggers and Decay:** Status effects can be triggered by various game events (e.g., start of turn, on damage dealt) and have different decay modes (e.g., end of turn, reduce stacks on trigger). All processing is handled through the command system for consistency.
