# Architecture

The project follows a command-driven architecture that emphasizes centralized state management, immutability, and separation of concerns. Key design patterns and components include:

- **Command Pattern:** All game actions are implemented as commands that are processed through the `GameCommandProcessor`. This provides consistent state management, error handling, and rollback capabilities.
- **Centralized State:** The `GameState` serves as the single source of truth for all game data, including spell state, status effects, and other game elements. State updates are immutable and validated.
- **Service Locator:** A central `ServiceLocator` class provides global access to singleton services like the `IEventBus`, `ILogger`, and the `GameCommandProcessor`. This decouples systems and simplifies dependency management.
- **Event Bus:** A simple `IEventBus` implementation allows for event-driven communication between different parts of the game. This reduces direct dependencies and promotes a reactive architecture.
- **Resource-Based Design:** Most game data, including cards, spells, and status effects, are implemented as Godot `Resource` objects. Resources create commands rather than executing directly, maintaining separation between data and behavior.
- **Logic Managers:** Static manager classes (`SpellLogicManager`, `StatusEffectLogicManager`) contain pure functions for business logic operations. These are called by commands to perform calculations and state transformations.
- **State-Driven Visuals:** Visual effects and UI updates are driven by state changes rather than direct method calls, ensuring consistency between game state and visual representation.
