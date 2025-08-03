# Status Effects

The status effect system is responsible for managing temporary and persistent effects on game entities.

- **`StatusEffectResource`**: A Godot `Resource` that defines a status effect, including its type, trigger condition, decay mode, and behavior.
- **`StatusEffectInstance`**: A runtime representation of a status effect that has been applied to an entity. It tracks the current number of stacks and other instance-specific data.
- **`StatusEffectManager`**: A service that manages all active `StatusEffectInstance` objects. It is responsible for applying new effects, triggering existing ones, and removing expired effects.
- **Triggers and Decay:** Status effects can be triggered by various game events (e.g., start of turn, on damage dealt) and have different decay modes (e.g., end of turn, reduce stacks on trigger).
