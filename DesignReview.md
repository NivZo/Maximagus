"""# Design Review

This document outlines a code review of the Maximagus project, focusing on key architectural and code quality aspects.

## 1. Backend (Logic) and Frontend (Visuals) Interaction

The project currently has a decent separation between logic and visuals in some areas, but there are opportunities for improvement, especially in the hand management system.

### 1.1. Hand Management System

The current implementation of the hand management system has several issues:

*   **Entangled Responsibilities:** The `Hand` node is responsible for both the visual representation of the hand (fanning, card positions) and some of the hand logic (drawing, discarding, reordering). The `HandManager` also contains hand logic, leading to a confusing and entangled separation of concerns.
*   **Direct Node Access:** The `HandManager` directly accesses the `Hand.Instance` singleton, creating a tight coupling between the two. This makes it difficult to test the `HandManager` in isolation and violates the principle of separation of concerns.
*   **State Management:** The state of the hand (the cards it contains) is managed by the `Hand` node, which is a `Control` node. This is not ideal, as the state should be managed by a non-visual class.

#### Recommendations:

1.  **Refactor `Hand` and `HandManager`:**
    *   The `Hand` node should be purely responsible for the visual representation of the hand. It should observe a data-only representation of the hand and update its visuals accordingly.
    *   The `HandManager` should be the single source of truth for the hand's state and logic. It should not be a `Node` and should not have any visual responsibilities.
    *   A new `HandModel` class should be created to represent the state of the hand (e.g., a list of `CardResource` objects). The `HandManager` will own this model.

2.  **Introduce a `HandController`:**
    *   A `HandController` class (or similar) should be responsible for mediating between the `HandManager` and the `Hand` node.
    *   The `HandController` would listen for events from the `HandManager` (e.g., `HandChanged`) and update the `Hand` node's visuals accordingly.
    *   The `HandController` would also listen for UI events from the `Hand` node (e.g., card clicked, card dragged) and translate them into actions on the `HandManager`.

3.  **Use the Event Bus for Communication:**
    *   The `HandManager` should publish events on the event bus when the hand changes (e.g., `HandChangedEvent`).
    *   The `HandController` will subscribe to these events and update the `Hand` view.
    *   This will decouple the `HandManager` from the `Hand` view, making the system more modular and testable.

### 1.2. Card System

The card system is in a better state than the hand system, with a clearer separation between the `Card` (Control), `CardLogic` (Button), and `CardVisual` (Control) nodes. However, there are still some areas for improvement:

*   **`CardLogic` Responsibilities:** The `CardLogic` class handles both input and some state management (e.g., `IsSelected`). This could be further separated.
*   **Event Usage:** The card system uses a mix of direct method calls and event bus events. A more consistent approach would be beneficial.

#### Recommendations:

1.  **Refine `CardLogic`:**
    *   Consider splitting the input handling logic from the state management logic into separate components.
    *   The `CardLogic` could be renamed to `CardInputHandler` to better reflect its primary responsibility.
    *   The card's state (`IsSelected`, `IsHovering`, etc.) could be managed by a separate `CardState` class.

2.  **Consistent Event Usage:**
    *   Establish clear guidelines for when to use the event bus versus direct method calls.
    *   For example, events that affect the global game state (e.g., `CardPlayedEvent`) should use the event bus, while events that only affect the card itself (e.g., `HoverStarted`) could be handled with C# events.

## 2. Event System and Communication

The project uses a simple event bus, which is a good foundation. However, its usage could be more consistent and widespread.

### 2.1. Event Bus Usage

The event bus is currently used for some key game events, but there are many other cases where direct method calls or singletons are used for communication between systems.

#### Recommendations:

1.  **Embrace the Event Bus:**
    *   Use the event bus as the primary means of communication between decoupled systems.
    *   For example, instead of the `GameInputManager` directly calling the `HandManager`, it could publish a `PlaySelectedCardsIntent` event, which the `HandManager` would then handle.
    *   This would further decouple the input handling from the game logic.

2.  **Define a Clear Event Hierarchy:**
    *   Create a clear and consistent naming convention for events.
    *   Consider grouping related events into namespaces or static classes.
    *   Document the purpose and payload of each event.

### 2.2. C# Events vs. Event Bus

The project uses both native C# events (delegates) and the event bus. The distinction between when to use each is not always clear.

#### Recommendations:

1.  **Establish Clear Guidelines:**
    *   **C# Events:** Use for communication within a single object or between tightly coupled objects (e.g., between a `Card` and its `CardVisual`).
    *   **Event Bus:** Use for communication between decoupled systems or for events that have a global impact on the game state.

## 3. Clean Code, SOLID, and Maintainability

The codebase is generally well-structured, but there are several areas where it could be improved to enhance readability, maintainability, and adherence to SOLID principles.

### 3.1. Single Responsibility Principle (SRP)

Some classes, like `Hand` and `CardLogic`, have multiple responsibilities, as discussed earlier.

#### Recommendations:

*   **Refactor Large Classes:** Break down large classes with multiple responsibilities into smaller, more focused classes.
*   **Apply SRP Consistently:** Ensure that each class has a single, well-defined purpose.

### 3.2. Dependency Inversion Principle (DIP)

The project uses a `ServiceLocator` for dependency injection, which is a good start. However, there are still some direct dependencies on concrete classes.

#### Recommendations:

*   **Depend on Abstractions:** Ensure that high-level modules depend on abstractions (interfaces) rather than concrete implementations.
*   **Use Dependency Injection:** Continue to use the `ServiceLocator` to inject dependencies, and consider using a more advanced dependency injection framework if the project grows in complexity.

### 3.3. Readability and Maintainability

The code is generally readable, but there are some areas that could be improved.

#### Recommendations:

*   **Consistent Naming:** Use a consistent naming convention for classes, methods, and variables.
*   **Add Comments:** Add comments to explain complex or non-obvious code.
*   **Refactor Complex Methods:** Break down long and complex methods into smaller, more manageable ones.
*   **Use `nameof`:** Use the `nameof` operator to avoid magic strings when referring to node names or other identifiers.

## 4. A New Architecture for Events and State

To eliminate confusion and create a robust, predictable, and maintainable system, we will restructure our event handling around three distinct communication channels. This approach separates player/system *actions* from authoritative *state changes* and purely *visual* feedback.

### 4.1. The Three Event Channels

We will implement and register three separate event bus interfaces in our `ServiceLocator`:

1.  **`IGameplayEventBus` (The "Action" Bus):**
    *   **Purpose:** This is the primary channel for communicating gameplay actions, intents, and commands. It's how different decoupled systems tell each other what they are *doing* or *want to do*.
    *   **Events:** Carries "intent" and "action" events. Examples: `PlayCardIntent`, `HandSubmitted`, `SpellResolved`, `EnemyDied`.
    *   **Publishers:** Any system can publish here. Typically UI Controllers (`CardLogic`, `HandController`) publish *intents*, and core logic Managers (`HandManager`, `SpellManager`) publish *actions*.
    *   **Subscribers:** Mostly core logic Managers that need to react to player intents or the actions of other managers.

2.  **`IGameStateEventBus` (The "Announcement" Bus):**
    *   **Purpose:** This bus has one, critical job: to announce that the core game state has officially changed. It is the single source of truth for state change notifications. This is a "read-only" bus for most of the application.
    *   **Events:** Carries "state changed" events. Examples: `TurnPhaseChanged`, `PlayerHealthUpdated`, `HandModified`.
    *   **Publishers:** **ONLY** the `GameStateManager` (or other designated state-owning classes). No other class is permitted to publish to this bus.
    *   **Subscribers:** Any system that needs to react to a change in the game's state. This includes UI elements (updating health bars), visual controllers (`HandVisual` reacting to `HandModified`), and managers (`StatusEffectManager` reacting to `TurnPhaseChanged`).

3.  **`IVisualEventBus` (The "UI" Bus):**
    *   **Purpose:** This channel is dedicated to communication *between* a logical component and its visual counterpart, or between different visual-only components. It handles events that are purely for presentation and do not affect game logic.
    *   **Events:** Carries UI-specific events. Examples: `CardHoverStarted`, `CardDragBegan`, `TooltipShowRequested`, `CardFanAnimationCompleted`.
    *   **Publishers:** UI controllers (`CardLogic`) and visual components (`HandVisual`).
    *   **Subscribers:** Visual components (`CardVisual`) and UI managers (`TooltipManager`).

### 4.2. The Golden Rules of Event Flow

This new structure is governed by a strict, unidirectional flow for state changes:

**Action -> State Change -> Announcement -> Reaction**

1.  **An action occurs:** A player clicks a card. The `CardLogic` publishes a `PlayCardIntent` to the `IGameplayEventBus`.
2.  **A manager handles the action:** The `HandManager` listens to the `IGameplayEventBus`, receives the `PlayCardIntent`, and validates it.
3.  **The manager updates the state:** The `HandManager` tells the `GameStateManager` to update the hand state.
4.  **The state is officially changed:** The `GameStateManager` modifies its internal state (e.g., removes the card from the hand list).
5.  **The state change is announced:** The `GameStateManager` publishes a `HandModified` event to the `IGameStateEventBus`.
6.  **Systems react to the announcement:**
    *   The `HandVisual` controller listens to the `IGameStateEventBus`, hears `HandModified`, and updates the on-screen cards.
    *   The `SpellManager` listens, hears `HandModified`, and checks if the played card triggers any special effects.

### 4.3. Benefits of This Architecture

*   **Clarity and Predictability:** The flow of data is always the same. It's easy to trace how an action leads to a state change and subsequent reactions.
*   **Single Source of Truth:** The `GameStateManager` is the undeniable authority on the game's state. The `IGameStateEventBus` is the official record of changes to that state.
*   **Decoupling:** Managers don't need to know about each other, they just listen for actions and state changes. Visuals are completely decoupled from the core logic.
*   **Reduced Noise:** The `IGameplayEventBus` isn't cluttered with state announcements, and the `IGameStateEventBus` isn't polluted with transient UI events.
*   **Testability:** Each component can be tested in isolation. A manager's logic can be tested by sending it events on the `IGameplayEventBus` and verifying that it correctly updates the `GameStateManager`.

This new structure will be implemented as part of the ongoing refactoring, starting with the Hand system.

## Conclusion

The Maximagus project has a solid foundation, but there are several areas where the architecture and code quality can be improved. By focusing on the recommendations outlined in this document, the project can become more modular, maintainable, and scalable in the long run.
""