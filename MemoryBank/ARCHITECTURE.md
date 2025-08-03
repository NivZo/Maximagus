# Architecture

The project follows a modular architecture that emphasizes separation of concerns and extensibility. Key design patterns and components include:

- **Service Locator:** A central `ServiceLocator` class provides global access to singleton services like the `IEventBus`, `ILogger`, and various managers. This decouples systems and simplifies dependency management.
- **Event Bus:** A simple `IEventBus` implementation allows for event-driven communication between different parts of the game. This reduces direct dependencies and promotes a reactive architecture.
- **Resource-Based Design:** Most game data, including cards, spells, and status effects, are implemented as Godot `Resource` objects. This allows designers to create and configure game elements directly in the Godot editor.
- **Managers:** Various manager classes (`TurnManager`, `HandManager`, `StatusEffectManager`) are responsible for specific domains of the game logic.
