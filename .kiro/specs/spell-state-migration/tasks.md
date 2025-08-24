# Implementation Plan

- [x] 1. Create spell state infrastructure





  - Create SpellState class with immutable properties for active spell tracking
  - Create ModifierData class to represent spell modifiers in state
  - Create SpellHistoryEntry class with card references and completion data
  - Write comprehensive unit tests for spell state classes
  - _Requirements: 1.1, 1.3, 4.1, 5.1_

- [x] 2. Create status effects state infrastructure






  - Create StatusEffectsState class with immutable active effects collection
  - Create StatusEffectInstanceData class to represent status effect instances in state
  - Write unit tests for status effects state classes
  - _Requirements: 2.1, 2.4_

- [x] 3. Extend GameState with new state sections





  - Add SpellState and StatusEffectsState properties to IGameStateData interface
  - Add WithSpell and WithStatusEffects methods to IGameStateData interface
  - Update GameState class to include new state sections and update methods
  - Update GameState validation logic to include spell and status effects validation
  - Update GameState.CreateInitial() to initialize new state sections
  - Write unit tests for extended GameState functionality
  - _Requirements: 1.1, 2.1, 7.1, 7.2_

- [x] 4. Implement spell logic manager





  - Create SpellLogicManager static class with damage calculation functions
  - Implement CalculateModifiedDamage function using current SpellContext logic
  - Implement ApplyDamageModifiers function with modifier consumption logic
  - Implement AddModifier function for adding modifiers to spell state
  - Implement UpdateProperty function for spell context property updates
  - Write comprehensive unit tests for all SpellLogicManager functions
  - _Requirements: 1.2, 4.3, 4.4, 8.1, 8.3_

- [x] 5. Implement status effect logic manager





  - Create StatusEffectLogicManager static class with status effect operations
  - Implement ApplyStatusEffect function using current StatusEffectManager logic
  - Implement TriggerEffects function for processing status effect triggers
  - Implement ProcessDecay function for handling status effect decay
  - Implement GetStacksOfEffect function for querying effect stacks
  - Write comprehensive unit tests for all StatusEffectLogicManager functions
  - _Requirements: 2.2, 2.3, 2.4, 8.1, 8.3_

- [x] 6. Create spell-related commands




  - Create StartSpellCommand to initialize spell state when casting begins
  - Create ExecuteCardActionCommand to process individual card actions
  - Create CompleteSpellCommand to finalize spell and move to history
  - Create UpdateSpellPropertyCommand for spell context property updates
  - Create AddSpellModifierCommand for adding modifiers to active spell
  - Write unit tests for all spell commands
  - _Requirements: 1.2, 3.1, 3.3, 5.1_

- [x] 7. Create status effect commands





  - Create ApplyStatusEffectCommand for adding/updating status effects
  - Create TriggerStatusEffectsCommand for processing status effect triggers
  - Create ProcessStatusEffectDecayCommand for end-of-turn decay handling
  - Write unit tests for all status effect commands
  - _Requirements: 2.2, 2.3, 3.2_

- [x] 8. Update ActionResource system for command integration





  - Modify ActionResource base class to remove SpellContext dependencies
  - Update ActionResource.GetPopUpEffectText to use IGameStateData parameter
  - Add CreateExecutionCommand method to ActionResource base class
  - Update DamageActionResource to create ExecuteCardActionCommand instances
  - Update ModifierActionResource to create AddSpellModifierCommand instances
  - Update StatusEffectActionResource to create ApplyStatusEffectCommand instances
  - Remove all SpellContext.Execute methods from action resources
  - Write unit tests for updated ActionResource implementations
  - _Requirements: 3.1, 3.3, 10.1, 10.4_

- [x] 9. Update SpellCardResource for command system





  - Modify SpellCardResource to work with new command-based execution
  - Add CreateExecutionCommands method to generate commands for all actions
  - Remove any direct SpellContext dependencies from SpellCardResource
  - Update any SpellCardResource subclasses for new execution model
  - Write unit tests for updated SpellCardResource functionality
  - _Requirements: 3.1, 10.1, 10.4_

- [x] 10. Update StatusEffectResource for state integration





  - Modify StatusEffectResource to work with centralized StatusEffectsState
  - Update any StatusEffectResource methods that directly manipulate local state
  - Ensure StatusEffectResource integrates properly with StatusEffectLogicManager
  - Write unit tests for updated StatusEffectResource functionality
  - _Requirements: 2.1, 10.1, 10.4_

- [x] 11. Implement spell processing command chain





  - Update SpellCastCommand to use StartSpellCommand instead of SpellProcessingManager
  - Create command chain for processing each card action sequentially
  - Implement proper command sequencing for spell execution flow
  - Add CompleteSpellCommand to finalize spell processing
  - Remove SpellProcessingManager.ProcessSpell method calls
  - Write integration tests for complete spell processing flow
  - _Requirements: 1.2, 3.1, 3.2, 10.2_

- [x] 12. Update card visuals for state-driven effects





  - Modify Card class to subscribe to GameState changes for popup effects
  - Implement OnGameStateChanged method to detect relevant state changes
  - Add logic to determine when card should show popup effects based on state
  - Create popup effects based on spell state changes rather than direct calls
  - Remove direct popup effect creation from SpellProcessingManager
  - Write tests for visual state integration
  - _Requirements: 6.2, 6.3, 9.1, 9.3_

- [x] 13. Integrate status effects with turn management



  - Update TurnStartCommand to trigger status effects using new command system
  - Update TurnEndCommand to process status effect decay using new commands
  - Replace StatusEffectManager.TriggerEffects calls with TriggerStatusEffectsCommand
  - Replace StatusEffectManager.ProcessEndOfTurnDecay with ProcessStatusEffectDecayCommand
  - Write integration tests for status effect turn integration
  - _Requirements: 2.2, 2.3, 2.5_

- [x] 14. Remove legacy SpellContext system





  - Delete SpellContext class and all its methods
  - Remove all imports and references to SpellContext throughout codebase
  - Remove SpellContext parameters from any remaining method signatures
  - Update any interfaces that reference SpellContext
  - Verify no SpellContext usage remains in the codebase
  - _Requirements: 10.1, 10.4, 10.5_

- [x] 15. Remove legacy StatusEffectManager local state





  - Remove local _activeEffects array from StatusEffectManager
  - Remove all local state management methods from StatusEffectManager
  - Update StatusEffectManager to use centralized state through commands
  - Remove any direct state manipulation methods from StatusEffectManager
  - Verify StatusEffectManager only uses centralized state access
  - _Requirements: 10.2, 10.4, 10.5_

- [x] 16. Clean up SpellProcessingManager





  - Remove direct spell execution logic from SpellProcessingManager
  - Remove popup effect creation logic from SpellProcessingManager
  - Update SpellProcessingManager to use command-based spell processing
  - Remove any unused methods and dependencies from SpellProcessingManager
  - Verify SpellProcessingManager integrates properly with new command system
  - _Requirements: 10.2, 10.4, 10.5_

- [x] 17. Update interfaces and service dependencies





  - Update ISpellProcessingManager interface for new command-based approach
  - Update IStatusEffectManager interface to remove local state methods
  - Review and update any other interfaces affected by the migration
  - Update service registrations if needed for new system
  - Verify all interface implementations match updated contracts
  - _Requirements: 8.4, 10.4, 10.5_

- [x] 18. Comprehensive integration testing





  - Write end-to-end tests for complete spell casting with multiple cards
  - Test status effect application, triggering, and decay through full game cycles
  - Test modifier application and consumption during spell casting
  - Test spell history recording with card references
  - Test visual effects creation through state changes
  - Verify identical behavior to original system through regression tests
  - _Requirements: 6.1, 6.4, 7.4_

- [x] 19. Performance and validation testing





  - Test state update performance with complex spells and many status effects
  - Verify state validation catches invalid configurations
  - Test command queue processing performance during spell casting
  - Test memory usage and cleanup of spell history
  - Verify error handling and recovery for failed commands
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 20. Refactor SpellLogicManager to use complete game state




  - Update CalculateModifiedDamage to receive IGameStateData instead of individual parameters
  - Update ApplyDamageModifiers to receive IGameStateData instead of individual parameters  
  - Update ProcessDamageAction to receive IGameStateData instead of status effect function
  - Remove Func<StatusEffectType, int> parameters from all SpellLogicManager methods
  - Update ExecuteCardActionCommand to pass complete game state to SpellLogicManager
  - Update all unit tests to use IGameStateData instead of individual parameters
  - Verify all damage calculations work correctly with the new approach
  - _Requirements: 8.1, 8.3, 8.4_



- [ ] 21. Final cleanup and code review
  - Remove any remaining unused imports, methods, or classes
  - Verify no legacy system code remains in the codebase
  - Review code for SOLID principles compliance and maintainability
  - Update any documentation or comments affected by the migration
  - Perform final code review to ensure system coherence and consistency
  - _Requirements: 8.4, 10.4, 10.5_