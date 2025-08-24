# Requirements Document

## Introduction

This feature enhances the pre-calculation system to create complete encounter state snapshots that include both spell state and status effect state changes. Currently, the pre-calculation system only calculates damage values in advance, but doesn't account for status effect stacks and other state changes that affect damage calculations. This enhancement will create an EncounterState that encompasses both SpellState and StatusEffectsState, enabling accurate pre-calculation of all action results including status effect interactions.

## Requirements

### Requirement 1

**User Story:** As a developer, I want an EncounterState that combines SpellState and StatusEffectsState, so that all encounter-related state is managed as a cohesive unit during spell processing.

#### Acceptance Criteria

1. WHEN an encounter begins THEN the system SHALL create an EncounterState containing both SpellState and StatusEffectsState
2. WHEN spell actions are processed THEN they SHALL update the complete EncounterState rather than individual state sections
3. WHEN accessing encounter data THEN the system SHALL provide unified access to both spell and status effect information through EncounterState
4. WHEN encounter state is updated THEN both spell and status effect changes SHALL be applied atomically
5. WHEN encounter state is validated THEN the system SHALL ensure consistency between spell state and status effect state

### Requirement 2

**User Story:** As a developer, I want pre-calculation to create complete EncounterState snapshots, so that all action results including status effect interactions are calculated in advance.

#### Acceptance Criteria

1. WHEN actions are pre-calculated THEN the system SHALL create a complete EncounterState snapshot for each action's resulting state
2. WHEN pre-calculating action results THEN status effect stacks and interactions SHALL be included in the calculations
3. WHEN pre-calculating subsequent actions THEN they SHALL use the previous action's EncounterState snapshot as the base reference
4. WHEN no previous snapshot exists THEN the system SHALL use the current game state's encounter state as the base reference
5. WHEN pre-calculation completes THEN each action SHALL have an associated EncounterState snapshot representing the state after execution

### Requirement 3

**User Story:** As a developer, I want action execution to use pre-calculated EncounterState snapshots, so that live execution applies the correct state changes without recalculating.

#### Acceptance Criteria

1. WHEN an action is executed live THEN it SHALL fetch the pre-calculated EncounterState snapshot for that action's timestamp
2. WHEN applying the snapshot THEN the current game state SHALL be updated to match the pre-calculated EncounterState
3. WHEN updating to the snapshot state THEN both spell state and status effect state SHALL be applied from the snapshot
4. WHEN no pre-calculated snapshot exists THEN the system SHALL throw an error indicating missing pre-calculation
5. WHEN snapshot application completes THEN all other game components SHALL see the updated encounter state

### Requirement 4

**User Story:** As a developer, I want status effect calculations included in pre-calculation, so that damage modifiers from status effects are accurately predicted and applied.

#### Acceptance Criteria

1. WHEN pre-calculating damage actions THEN status effect stacks SHALL be included in damage modifier calculations
2. WHEN status effects modify damage THEN the pre-calculated results SHALL reflect these modifications
3. WHEN status effects are applied during pre-calculation THEN the resulting status effect state SHALL be included in the snapshot
4. WHEN status effects trigger during pre-calculation THEN their effects SHALL be calculated and included in the snapshot
5. WHEN status effects decay during pre-calculation THEN the updated stacks SHALL be reflected in the snapshot

### Requirement 5

**User Story:** As a developer, I want timestamp-based snapshot management, so that actions can be executed in the correct order with the appropriate state context.

#### Acceptance Criteria

1. WHEN creating snapshots THEN each SHALL be associated with a specific action timestamp
2. WHEN retrieving snapshots THEN the system SHALL return the snapshot for the requested timestamp
3. WHEN snapshots are stored THEN they SHALL be ordered by timestamp for sequential access
4. WHEN clearing snapshots THEN the system SHALL remove snapshots for completed or cancelled spells
5. WHEN managing snapshot memory THEN the system SHALL prevent excessive memory usage from accumulated snapshots

### Requirement 6

**User Story:** As a developer, I want legacy pre-calculation methodology adapted or removed, so that the system uses only the new EncounterState-based approach.

#### Acceptance Criteria

1. WHEN the new system is active THEN all previous damage-only pre-calculation SHALL be replaced with EncounterState snapshots
2. WHEN legacy pre-calculation methods exist THEN they SHALL be removed or adapted to work with EncounterState
3. WHEN pre-calculation is requested THEN only the new EncounterState-based system SHALL be used
4. WHEN reviewing the codebase THEN no legacy pre-calculation code SHALL remain that bypasses EncounterState
5. WHEN testing pre-calculation THEN all tests SHALL use the new EncounterState-based approach

### Requirement 7

**User Story:** As a developer, I want EncounterState to be immutable and validated, so that state consistency is maintained throughout the pre-calculation and execution process.

#### Acceptance Criteria

1. WHEN EncounterState is created THEN it SHALL be immutable with proper validation
2. WHEN EncounterState is updated THEN new instances SHALL be created rather than modifying existing ones
3. WHEN EncounterState changes are applied THEN validation SHALL ensure state consistency
4. WHEN invalid EncounterState is detected THEN the system SHALL provide clear error messages
5. WHEN debugging state issues THEN developers SHALL have access to complete EncounterState inspection tools

### Requirement 8

**User Story:** As a developer, I want seamless integration with existing systems, so that the EncounterState enhancement doesn't break current functionality.

#### Acceptance Criteria

1. WHEN EncounterState is implemented THEN existing spell processing SHALL continue to work without modification
2. WHEN status effect processing occurs THEN it SHALL integrate seamlessly with the new EncounterState system
3. WHEN visual effects are triggered THEN they SHALL work correctly with EncounterState-based updates
4. WHEN game state is accessed THEN existing interfaces SHALL continue to provide access to spell and status effect data
5. WHEN the system is deployed THEN no existing functionality SHALL be broken by the EncounterState changes

### Requirement 9

**User Story:** As a developer, I want the system to follow SOLID principles, so that the code remains maintainable, testable, and extensible.

#### Acceptance Criteria

1. WHEN implementing EncounterState THEN the Single Responsibility Principle SHALL be maintained with clear separation of concerns
2. WHEN extending pre-calculation functionality THEN the Open/Closed Principle SHALL be followed to allow extension without modification
3. WHEN creating interfaces THEN the Interface Segregation Principle SHALL ensure clients depend only on methods they use
4. WHEN implementing dependencies THEN the Dependency Inversion Principle SHALL be applied with proper abstraction layers
5. WHEN existing infrastructure violates SOLID principles THEN it SHALL be refactored to comply with these principles

### Requirement 10

**User Story:** As a developer, I want implementation sequencing that prioritizes core functionality, so that the main EncounterState purpose is fulfilled before addressing extraneous adjustments.

#### Acceptance Criteria

1. WHEN implementing the feature THEN core EncounterState functionality SHALL be completed before any infrastructure adjustments
2. WHEN core functionality is complete THEN existing infrastructure SHALL be evaluated for SOLID principles compliance
3. WHEN SOLID violations are identified THEN they SHALL be addressed after the main EncounterState system is working
4. WHEN making extraneous adjustments THEN they SHALL not interfere with the primary EncounterState functionality
5. WHEN sequencing implementation tasks THEN core requirements SHALL take priority over infrastructure improvements