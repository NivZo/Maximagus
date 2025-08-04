# AI Code Review Instructions: Card Game Architecture Analysis

## Overview
You are tasked with conducting a comprehensive code review of a card-based video game project. This document provides detailed instructions on how to analyze the codebase, what to look for, and how to structure your findings.

## Project Context & Goals

### Game Flow Summary
The game implements a card-based mechanic with the following core loop:
- Players can hover, drag, reorder, and toggle cards in a hand at the bottom of the screen
- Players can Play or Discard selected cards
- When played, cards provide visual feedback and enact their effects
- After effects resolve, cards are discarded and new cards are drawn
- If discarded directly, cards are removed without effects and new cards are drawn

## Expected Architecture Flow

> **CRITICAL**: This flow diagram represents the **IDEAL ARCHITECTURAL PATTERN**, not a literal implementation specification. The actual project may have different naming conventions, additional intermediate layers, or variations in implementation while still following these core principles. Assess whether the project's actual flow aligns with these patterns, not whether it matches exactly.

The review should validate that the project follows this conceptual flow:

```
USER INPUT → UI Layer (publishes Input Events)
    ↓
GAMECONTROLLER (subscribes to Input Events)
    ↓ (validates & translates)
COMMAND EVENTS (published by GameController)
    ↓
GAMELOGIC (subscribes to Command Events)
    ↓ (executes & updates state)
GAME EVENTS (published by GameLogic)
    ↓
STATE MACHINE (subscribes to relevant Game Events)
    ↓ (transitions states)
STATE ACTIONS (OnEnter/OnExit)
    ↓
MANAGER CALLS (direct) + STATE EVENTS (broadcast)
    ↓
VISUAL SYSTEMS (subscribe to Game Events + State Events)
```

**Key Validation Points:**
- No system bypasses this logical flow (regardless of exact naming)
- Events flow in one direction through the chain (whatever the actual event types are)
- States use direct manager calls when possible, events when necessary (however managers are implemented)
- Visual systems never trigger game logic or state changes (whatever the visual architecture is)

## Code Review Process

### Phase 1: Architecture Overview
First, gain a high-level understanding of the project structure:

1. **Identify Core Systems**
   - Locate the state machine implementation
   - Find the event bus/messaging system
   - Identify UI/visual layer components
   - Locate game logic and state management
   - Find card-related classes and systems

2. **Map System Boundaries**
   - Document how systems communicate with each other
   - Identify direct dependencies vs event-based communication
   - Note any circular dependencies or tight coupling

3. **Understand Data Flow**
   - Trace how user input becomes game actions
   - Follow the path from game events to visual updates
   - Map state transition triggers and flows

### Phase 2: Detailed Analysis

#### State Machine Review
**Look for:**
- How states are defined and organized
- State transition logic and validation
- OnEnter/OnExit method implementations
- Manager class integration and direct calls
- Event broadcasting patterns

**Evaluate using the State Action Rules:**

**DIRECT MANAGER CALLS (Preferred when possible)**
States should directly call manager methods when:
- The functionality is exposed through a dedicated manager class
- The action is synchronous and deterministic
- The state has clear ownership of when this should happen
- The action doesn't need to happen in parallel with other systems

```csharp
// GOOD: Direct manager calls (EXAMPLE - adapt to actual project structure)
public class SpellCastState : GameState {
    public override void OnEnter() {
        uiManager.ShowSpellCastOverlay();
        audioManager.PlaySpellCastMusic();
        effectManager.PrepareSpellEffects();
    }
}
```

**EVENT BROADCASTING (Use when required)**
States should broadcast events when:
- Multiple systems need to react simultaneously/in parallel
- Systems need to react agnostically (don't care about the specific state)
- The reaction order is flexible or needs coordination
- External systems (analytics, logging, mods) might want to hook in

```csharp
// GOOD: Event broadcasting for parallel/agnostic reactions (EXAMPLE - adapt to actual project)
public class TurnEndState : GameState {
    public override void OnEnter() {
        // Direct calls for state-specific logic
        gameLogic.ProcessTurnEnd();
        
        // Events for parallel/agnostic reactions
        eventBus.Publish(new TurnEndedEvent(currentPlayer, turnNumber));
    }
}
```

> **NOTE**: The code examples above are **ILLUSTRATIVE ONLY**. The actual project may use different class names, method signatures, and event structures. Focus on the **PATTERNS** demonstrated rather than expecting these exact implementations.

**ANTI-PATTERNS TO FLAG:**
- States broadcasting events for simple manager calls
- States calling managers directly when parallel reactions are needed
- Mixed patterns without clear reasoning

#### Event System Review
**Look for:**
- Event types and categorization against the defined taxonomy
- Publisher/subscriber relationships and their appropriateness
- Event subscription patterns and timing
- Event naming conventions and consistency

**Evaluate against these concrete event categories:**

> **IMPORTANT**: The event names and scenarios below are **ABSTRACTED EXAMPLES** meant as guidelines. DO NOT expect the actual project to use these exact event names or implement these specific scenarios. Instead, use these patterns to understand the principles and assess how the real project's events align with these architectural categories.

**INPUT EVENTS** (UI Layer → Game Controller)
- *Examples*: `CardHovered`, `CardSelected`, `CardDeselected`
- *Examples*: `PlayButtonPressed`, `DiscardButtonPressed`
- *Examples*: `HandReordered`, `CardDragStarted`, `CardDragEnded`
- **Rule**: Only UI components publish these. Only GameController subscribes.
- **Assess**: Do the project's input events follow this pattern? Are they properly scoped to UI interactions?

**COMMAND EVENTS** (GameController → Game Logic)
- *Examples*: `PlayCardsRequested`, `DiscardCardsRequested`
- *Examples*: `EndTurnRequested`, `DrawCardsRequested`
- **Rule**: Only GameController publishes these. Only GameLogic subscribes.
- **Assess**: Does the project translate input into command-style events? Is this layer present and working correctly?

**GAME EVENTS** (Game Logic → Everyone)
- *Examples*: `CardEffectTriggered`, `PlayerHealthChanged`, `CardDrawn`
- *Examples*: `EffectResolved`, `TurnEnded`, `GameStateChanged`
- **Rule**: Only GameLogic publishes these. Multiple systems can subscribe.
- **Assess**: Are game state changes properly broadcast? Do multiple systems appropriately subscribe to these?

**STATE EVENTS** (State Machine → Everyone)
- *Examples*: `StateEntered`, `StateExited`, `TransitionStarted`
- **Rule**: Only StateMachine publishes these. Multiple systems can subscribe.
- **Assess**: Does the state machine broadcast transitions when appropriate? Are these events used correctly?

**VISUAL EVENTS** (Visual Systems → Visual Systems)
- *Examples*: `AnimationCompleted`, `EffectVisualizationFinished`
- *Examples*: `UITransitionDone`, `SoundEffectEnded`
- **Rule**: Visual/Audio systems only. Should not affect game logic.
- **Assess**: Are visual completion events properly isolated from game logic? Do they stay within the visual domain?

#### Responsibility Distribution
**Look for:**
- Manager class implementations and their scope
- Direct method calls vs event-based communication patterns
- Command pattern usage and flow
- System boundary respect

**Evaluate against these concrete responsibility rules:**

> **IMPORTANT**: The system names and method examples below are **CONCEPTUAL GUIDELINES**. The actual project may use different naming conventions, class structures, or organizational patterns. Use these as a framework to understand the intended separation of concerns and assess how the real project aligns with these principles.

**GAMECONTROLLER RESPONSIBILITIES:**
- Receives all input events from UI
- Validates player actions against current game state
- Translates input into command events
- Manages state machine transitions
- **Cannot**: Directly modify game state, handle UI updates, or bypass state machine
- **Assess**: Does the project have a clear controller layer? Are these boundaries respected?

**GAMELOGIC RESPONSIBILITIES:**
- Executes all command events
- Maintains authoritative game state
- Publishes game events for state changes
- Calculates card effects and game rules
- **Cannot**: Handle UI concerns, manage state transitions, or respond to input events
- **Assess**: Is game logic properly isolated? Does it maintain state authority correctly?

**STATE MACHINE RESPONSIBILITIES:**
- Manages current game phase
- Validates state transitions
- Provides state-specific behavior through OnEnter/OnExit
- **Cannot**: Contain game logic, handle input directly, or modify game state
- **Assess**: How well does the state machine coordinate without overstepping into other domains?

**MANAGER CLASSES (UIManager, AudioManager, EffectManager, etc.):**
- Provide clean interfaces for their domain
- Handle implementation details internally
- Should be callable directly by states when appropriate
- **Cannot**: Trigger state changes or contain game logic
- **Assess**: Are manager-style classes present? Do they provide appropriate abstractions?

**UI LAYER RESPONSIBILITIES:**
- Handles user input and publishes input events
- Subscribes to game events for visual updates
- Manages visual state and animations
- **Cannot**: Contain game logic, trigger state changes directly, or access game state without events
- **Assess**: Is UI properly decoupled from game logic? Does it communicate correctly through events?

**VISUAL SYSTEMS RESPONSIBILITIES:**
- React to game events for visual feedback
- Manage their own visual state and timing
- Publish completion events when needed for sequencing
- **Cannot**: Affect game state or trigger gameplay actions
- **Assess**: Are visual concerns properly isolated? Do visual systems stay within their boundaries?

#### Card System Implementation
**Look for:**
- Card data structures and behavior
- Effect system implementation
- Visual feedback mechanisms
- Card selection and interaction handling

**Evaluate:**
- How well does the card system integrate with the state machine?
- Is card effect resolution properly sequenced?
- Are card visuals decoupled from card logic?
- Is the system extensible for new card types?

### Phase 3: Problem Identification

#### Common Anti-Patterns to Flag
- **Scattered Responsibilities**: State changes triggered from multiple places
- **Event Overuse**: Everything is an event when direct calls would be cleaner
- **Event Underuse**: Tight coupling where events would provide better separation
- **State Machine Bypass**: Logic circumventing the state machine
- **Mixed Concerns**: UI logic mixed with game logic
- **Unclear Ownership**: Multiple systems claiming responsibility for the same actions

#### Architecture Smells
- Circular dependencies between systems
- Deep call chains that cross multiple system boundaries
- Systems that know too much about other systems' internals
- Event chains that are hard to trace or debug
- States that are either too heavyweight or too lightweight

## Review Document Structure

Your review should be organized as follows:

### 1. Executive Summary
- Overall architectural health (Strong/Good/Needs Work/Poor)
- Top 3 strengths of the current implementation
- Top 3 areas needing immediate attention
- General trajectory assessment (improving/stable/degrading)

### 2. System Analysis
For each major system:
- **Current State**: What exists and how it works
- **Alignment with Goals**: How well it matches the intended architecture
- **Strengths**: What's working well
- **Weaknesses**: What needs improvement
- **Recommendations**: Specific actionable improvements

### 3. Interaction Analysis
- **State Machine Usage**: How well the state machine is being utilized
- **Event Flow**: Quality and organization of event-driven communication
- **Responsibility Boundaries**: How well concerns are separated
- **Code Flow Traceability**: How easy it is to follow game logic execution

### 4. Specific Findings
- **Good Examples**: Code snippets that exemplify good architecture
- **Problem Areas**: Specific files/classes/methods that need attention
- **Inconsistencies**: Where the code doesn't follow established patterns
- **Missing Pieces**: Architectural components that should exist but don't

### 5. Recommendations
#### Immediate Actions (High Priority)
- Critical architectural issues that should be addressed first
- Specific refactoring suggestions with rationale

#### Medium-Term Improvements
- Enhancements that would improve maintainability
- Architectural refinements that align with the project goals

#### Long-Term Considerations
- Scalability concerns
- Future extensibility improvements
- Technical debt that should be planned for

### 6. Implementation Guidance
For each major recommendation:
- **Why**: The problem being solved
- **What**: The specific change being proposed
- **How**: Step-by-step approach to implementation
- **Risk Assessment**: Potential complications or considerations

## Key Success Metrics

Evaluate the codebase against these criteria:

### Clarity & Maintainability
- Can you easily trace the flow from user input to visual output?
- Are system responsibilities clearly defined and respected?
- Would a new developer understand the architecture quickly?

### Flexibility & Extensibility
- How easy would it be to add new card types?
- Can new game states be added without major refactoring?
- Is the event system set up to handle future requirements?

### Architectural Integrity
- Does the code follow the intended separation of concerns?
- Is the state machine being properly utilized?
- Are events being used appropriately (not over/under-used)?

### Code Quality
- Are there clear patterns being followed consistently?
- Is the code organized in a logical, discoverable way?
- Are there appropriate abstractions without over-engineering?

## Dynamic Project Considerations

Remember that this is an active, evolving project:

1. **Prioritize Practicality**: Recommendations should be implementable without complete rewrites
2. **Consider Current Momentum**: Work with existing patterns where they're working well
3. **Focus on Foundations**: Ensure core architectural decisions are solid before suggesting feature additions
4. **Balance Ideal vs Reality**: The perfect architecture may not be achievable immediately - suggest incremental improvements
5. **Account for Learning**: The developer is actively learning - provide educational context for recommendations

## Deliverable
Produce a comprehensive review document following the structure above. The review should be actionable, educational, and respectful of the current project state while clearly identifying paths toward the architectural goals discussed.

Remember: The goal is not to criticize but to provide constructive guidance that helps the project evolve toward a clean, maintainable, and well-structured architecture that serves the game's requirements effectively.