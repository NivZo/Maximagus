# Requirements Document

## Introduction

This feature migrates the spell processing and status effect management systems from their current local state management approach to use the centralized GameState system managed by the GameCommandProcessor. The migration will maintain identical frontend behavior while restructuring the backend to use command-driven state updates, enabling better state consistency, debugging capabilities, and system integration.

## Requirements

### Requirement 1

**User Story:** As a developer, I want spell processing to use centralized state management, so that spell context and progress are tracked in the main game state rather than in isolated local variables.

#### Acceptance Criteria

1. WHEN a spell is cast THEN the system SHALL create spell state within the main GameState instead of using a local SpellContext object
2. WHEN spell actions are executed THEN each action SHALL update the centralized spell state through commands rather than modifying local context directly
3. WHEN spell processing completes THEN the spell context SHALL be transferred to spell history state and cleared from active spell state
4. WHEN spell processing encounters errors THEN the centralized spell state SHALL be properly cleaned up through the command system
5. WHEN accessing spell context data THEN the system SHALL read from the centralized GameState rather than local SpellContext properties

### Requirement 2

**User Story:** As a developer, I want status effects to be managed through centralized state, so that status effect data persists properly between spells and integrates with the command system.

#### Acceptance Criteria

1. WHEN status effects are applied THEN they SHALL be stored in a dedicated StatusEffectsState section of the main GameState
2. WHEN status effects are triggered THEN the system SHALL update the centralized state through commands rather than modifying local arrays
3. WHEN status effects decay or expire THEN the state changes SHALL be processed through the command system
4. WHEN querying status effect stacks THEN the system SHALL read from the centralized GameState rather than local StatusEffectManager arrays
5. WHEN status effects persist between spells THEN they SHALL remain in the centralized state across spell casting cycles

### Requirement 3

**User Story:** As a developer, I want card actions to be transformed into commands, so that each card's effects are processed through the unified command system with proper state tracking.

#### Acceptance Criteria

1. WHEN a card action is executed THEN it SHALL be converted into a specific command type that updates the relevant state sections
2. WHEN multiple card actions are chained THEN they SHALL be queued as sequential commands in the GameCommandProcessor
3. WHEN a card action modifies spell context THEN it SHALL update the centralized spell state through a command
4. WHEN a card action applies status effects THEN it SHALL use commands to update the StatusEffectsState
5. WHEN a card action deals damage THEN it SHALL update damage tracking in the centralized spell state through commands

### Requirement 4

**User Story:** As a developer, I want modifiers to be ephemeral and managed in spell state, so that they don't persist between casts but are properly tracked during spell execution.

#### Acceptance Criteria

1. WHEN modifiers are applied during spell casting THEN they SHALL be stored in the active spell state section
2. WHEN a spell completes THEN all active modifiers SHALL be cleared from the spell state
3. WHEN modifiers are consumed THEN the spell state SHALL be updated to reflect the consumption through commands
4. WHEN calculating modified damage THEN the system SHALL read active modifiers from the centralized spell state
5. WHEN a new spell begins THEN the modifier section of spell state SHALL start empty

### Requirement 5

**User Story:** As a developer, I want spell history to be maintained in state, so that completed spells and their effects are tracked for debugging and potential future features.

#### Acceptance Criteria

1. WHEN a spell completes THEN its final context SHALL be moved to a spell history section in GameState
2. WHEN accessing spell history THEN the system SHALL provide read-only access to completed spell data
3. WHEN spell history grows large THEN the system SHALL maintain a reasonable number of recent spell records
4. WHEN debugging spell issues THEN developers SHALL be able to inspect the spell history from the centralized state
5. WHEN spell processing encounters errors THEN the partial context SHALL still be recorded in spell history with appropriate error status

### Requirement 6

**User Story:** As a player, I want the visual spell casting experience to remain unchanged, so that the migration doesn't affect gameplay or user interface behavior.

#### Acceptance Criteria

1. WHEN casting spells THEN the visual animations and timing SHALL remain identical to the current implementation
2. WHEN card effects trigger THEN the popup effects SHALL display the same information and styling as before
3. WHEN status effects are applied THEN any visual indicators SHALL continue to work as expected
4. WHEN spell processing occurs THEN the player SHALL experience the same interaction flow and feedback
5. WHEN errors occur during spell casting THEN the user SHALL receive the same error handling and recovery behavior

### Requirement 7

**User Story:** As a developer, I want the new state structure to be validated and consistent, so that the centralized state management prevents invalid game states and provides clear error reporting.

#### Acceptance Criteria

1. WHEN spell state is updated THEN the GameState validation SHALL ensure spell context consistency
2. WHEN status effects are modified THEN the state validation SHALL prevent invalid status effect configurations
3. WHEN commands are executed THEN they SHALL validate state transitions before applying changes
4. WHEN state becomes invalid THEN the system SHALL provide clear error messages indicating the validation failure
5. WHEN debugging state issues THEN developers SHALL have access to comprehensive state inspection tools through the GameState interface
###
 Requirement 8

**User Story:** As a developer, I want spell logic separated from orchestration, so that spell interactions are maintainable, testable, and follow SOLID principles.

#### Acceptance Criteria

1. WHEN spell interactions are processed THEN they SHALL be handled by dedicated manager classes with static functions that receive state data and return outcomes
2. WHEN commands execute spell logic THEN they SHALL delegate to manager functions rather than containing business logic directly
3. WHEN modifier and damage calculations occur THEN they SHALL use the same logic as the current SpellContext but operate on state objects
4. WHEN extending spell functionality THEN developers SHALL be able to add new interactions without modifying existing command classes
5. WHEN testing spell logic THEN the manager functions SHALL be independently testable without requiring the full command system

### Requirement 9

**User Story:** As a developer, I want visual effects to be driven by state changes, so that card visuals respond to centralized state updates rather than direct method calls.

#### Acceptance Criteria

1. WHEN spell state changes occur THEN card visuals SHALL detect these changes and create appropriate popup effects
2. WHEN status effects are applied THEN the visual indicators SHALL be updated based on state change notifications
3. WHEN damage is dealt THEN popup effects SHALL be created by card visuals reading from the updated spell state
4. WHEN spell processing completes THEN visual cleanup SHALL occur based on state transitions rather than direct calls
5. WHEN debugging visual issues THEN developers SHALL be able to trace visual effects back to specific state changes

### Requirement 10

**User Story:** As a developer, I want complete removal of legacy systems, so that the codebase is clean, maintainable, and doesn't contain unused or redundant code.

#### Acceptance Criteria

1. WHEN the migration is complete THEN all SpellContext class usage SHALL be removed from the codebase
2. WHEN the new system is active THEN the old StatusEffectManager local state management SHALL be completely removed
3. WHEN commands are implemented THEN there SHALL be no fallback or adapter code to support legacy systems
4. WHEN the refactor is finished THEN all unused imports, methods, and classes related to the old systems SHALL be deleted
5. WHEN reviewing the code THEN there SHALL be no migration bridges, compatibility layers, or dual-system support code