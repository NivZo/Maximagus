# Implementation Plan

## Chunk 1: Core EncounterState Infrastructure

- [x] 1. Build EncounterState foundation and snapshot management





  - Create EncounterState class with immutable properties for spell and status effect state
  - Implement WithSpell, WithStatusEffects, WithTimestamp, WithActionIndex, and WithBoth methods
  - Add FromGameState static method to create EncounterState from IGameStateData
  - Add ApplyToGameState method to apply EncounterState to IGameStateData
  - Implement validation logic in IsValid method
  - Create EncounterStateSnapshot class with ActionKey, ResultingState, ActionResult, and CreatedAt properties
  - Implement EncounterSnapshotManager static class for snapshot storage and retrieval
  - Implement StoreSnapshots, GetSnapshotForAction, GetAllSnapshots, ClearSnapshots methods
  - Create GameStateExtensions static class with GetEncounterState and WithEncounterState methods
  - Write comprehensive unit tests for all new infrastructure classes
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 8.4_

## Chunk 2: Enhanced Logic Managers and Pre-Calculation

- [x] 2. Update logic managers for EncounterState and implement snapshot-based pre-calculation





  - Add PreCalculateActionWithSnapshot method to SpellLogicManager that creates complete EncounterStateSnapshot
  - Add PreCalculateSpellWithSnapshots method that generates snapshots for entire spell sequence
  - Update PreCalculateActionResult and ApplyDamageModifiers methods to work with EncounterState
  - Add ApplyEncounterSnapshot method for applying snapshots to game state
  - Add ApplyStatusEffectToEncounter, TriggerEffectsInEncounter, ProcessDecayInEncounter methods to StatusEffectLogicManager
  - Update existing StatusEffectLogicManager methods to work seamlessly with EncounterState-based calculations
  - Ensure status effect stacks are included in damage modifier calculations during pre-calculation
  - Update PerChill damage calculations to use status effect state from EncounterState
  - Write unit tests for all new and updated logic manager methods
  - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 4.5, 8.2_

## Chunk 3: Command System Integration and Execution

- [x] 3. Implement snapshot-based commands and integrate with spell processing





  - Create PreCalculateSpellCommand class that generates EncounterState snapshots for all spell actions
  - Create ApplyEncounterSnapshotCommand class for applying pre-calculated snapshots
  - Update ExecuteCardActionCommand to fetch and apply pre-calculated EncounterState snapshots
  - Replace existing damage calculation logic with snapshot application in ExecuteCardActionCommand
  - Update SpellCastCommand to execute PreCalculateSpellCommand before action execution
  - Modify spell processing sequence to use snapshot-based execution
  - Add proper error handling for missing snapshots and invalid state transitions
  - Ensure atomic application of both spell and status effect state changes
  - Add logging and debugging support for snapshot-based processing
  - Write unit and integration tests for all command updates and snapshot-based execution
  - _Requirements: 2.1, 2.2, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 5.4, 8.1_

## Chunk 4: Legacy Cleanup, SOLID Compliance, and Optimization

- [x] 4. Remove legacy systems, ensure SOLID compliance, and optimize performance







  - Remove ActionExecutionResult storage from SpellState class
  - Remove PreCalculatedActions property and related methods from SpellState
  - Remove old damage-only pre-calculation methods that bypass EncounterState
  - Update all references to use new EncounterState-based snapshot system
  - Review all new classes for SOLID principles compliance (SRP, OCP, ISP, DIP)
  - Refactor any existing infrastructure that violates SOLID principles
  - Implement memory management and cleanup for expired snapshots
  - Add automatic cleanup triggers when spells complete or are cancelled
  - Optimize snapshot creation performance for complex spells with many actions
  - Implement efficient snapshot storage and retrieval mechanisms
  - Write end-to-end integration tests for complete spell casting with EncounterState snapshots
  - Write performance tests and regression tests to validate system improvements
  - Perform final code review and cleanup to ensure system coherence and maintainability
  - _Requirements: 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.3, 8.1, 8.2, 8.3, 8.5, 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5_