# Refactoring Implementation Task List
## Spell, Status Effects, Commands & State Systems

### Document Version
- **Version**: 1.0
- **Date**: August 25, 2025
- **Author**: Technical Implementation Team
- **Status**: Ready for Execution

---

## Overview

This document provides a detailed, prioritized task list for implementing the refactoring plan. Each task includes specific implementation steps, acceptance criteria, and dependencies.

### Timeline Summary
- **Phase 1 (Foundation)**: Week 1 - DI Infrastructure & Interface Extraction
- **Phase 2 (Services)**: Weeks 2-3 - Domain Services & Command Consolidation  
- **Phase 3 (State)**: Weeks 4-6 - State Management & Performance Optimization

---

## Phase 1: Foundation (Week 1)

### Task 1.1: Implement Dependency Injection Container
**Priority**: Critical  
**Estimated Time**: 4 hours  
**Dependencies**: None

#### Implementation Steps:
1. Create `Scripts/Infrastructure/DI/IServiceContainer.cs`
2. Create `Scripts/Infrastructure/DI/ServiceContainer.cs`
3. Create `Scripts/Infrastructure/DI/ServiceDescriptor.cs`
4. Create `Scripts/Infrastructure/DI/ServiceLifetime.cs`
5. Add unit tests in `Tests/Infrastructure/DI/ServiceContainerTests.cs`

#### Code Template:
```csharp
// IServiceContainer.cs
namespace Maximagus.Infrastructure.DI;

public interface IServiceContainer
{
    void Register<TInterface, TImplementation>() where TImplementation : TInterface;
    void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface;
    void RegisterFactory<T>(Func<IServiceContainer, T> factory);
    T Resolve<T>();
    object Resolve(Type type);
}
```

#### Acceptance Criteria:
- [ ] Container can register transient services
- [ ] Container can register singleton services
- [ ] Container can resolve dependencies with constructor injection
- [ ] Container throws meaningful exceptions for missing registrations
- [ ] All unit tests pass

---

### Task 1.2: Extract Manager Interfaces
**Priority**: Critical  
**Estimated Time**: 6 hours  
**Dependencies**: None

#### Implementation Steps:
1. Create `Scripts/Interfaces/Managers/ISpellLogicManager.cs`
2. Create `Scripts/Interfaces/Managers/IStatusEffectLogicManager.cs`
3. Create `Scripts/Interfaces/Managers/IEncounterSnapshotManager.cs`
4. Create `Scripts/Interfaces/Managers/IQueuedActionsManager.cs`
5. Update existing managers to implement interfaces
6. Add XML documentation to all interface methods

#### Files to Modify:
- `Scripts/Implementations/Managers/SpellLogicManager.cs` - Add `: ISpellLogicManager`
- `Scripts/Implementations/Managers/StatusEffectLogicManager.cs` - Add `: IStatusEffectLogicManager`
- `Scripts/Implementations/Managers/EncounterSnapshotManager.cs` - Add `: IEncounterSnapshotManager`
- `Scripts/Implementations/Managers/QueuedActionsManager.cs` - Add `: IQueuedActionsManager`

#### Acceptance Criteria:
- [ ] All managers implement their respective interfaces
- [ ] No static dependencies remain in interfaces
- [ ] All existing functionality preserved
- [ ] Code compiles without errors

---

### Task 1.3: Create Service Configuration
**Priority**: High  
**Estimated Time**: 3 hours  
**Dependencies**: Tasks 1.1, 1.2

#### Implementation Steps:
1. Create `Scripts/Infrastructure/DI/ServiceConfiguration.cs`
2. Register all existing services
3. Create service initialization in `Main.cs`
4. Add configuration validation

#### Code Template:
```csharp
// ServiceConfiguration.cs
namespace Maximagus.Infrastructure.DI;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceContainer container)
    {
        // Core Services
        container.RegisterSingleton<ILogger, GodotLogger>();
        container.RegisterSingleton<IEventBus, SimpleEventBus>();
        container.RegisterSingleton<IGameCommandProcessor, GameCommandProcessor>();
        
        // Managers
        container.RegisterSingleton<ISpellLogicManager, SpellLogicManager>();
        container.RegisterSingleton<IStatusEffectLogicManager, StatusEffectLogicManager>();
        // ... more registrations
    }
}
```

#### Acceptance Criteria:
- [ ] All services registered in container
- [ ] Application starts without ServiceLocator errors
- [ ] Services resolve correctly from container

---

### Task 1.4: Remove SpellContextAdapter
**Priority**: High  
**Estimated Time**: 4 hours  
**Dependencies**: Task 1.2

#### Implementation Steps:
1. Analyze `Scripts/State/SpellContextAdapter.cs` usage
2. Create migration path for each usage
3. Update dependent code to use direct state access
4. Delete SpellContextAdapter.cs
5. Update tests affected by removal

#### Files to Modify:
- All files currently using SpellContextAdapter
- Related unit tests

#### Acceptance Criteria:
- [ ] SpellContextAdapter.cs deleted
- [ ] No compilation errors
- [ ] All tests pass
- [ ] No functional regressions

---

### Task 1.5: Create Migration Bridge
**Priority**: Medium  
**Estimated Time**: 2 hours  
**Dependencies**: Tasks 1.1, 1.3

#### Implementation Steps:
1. Create `Scripts/Infrastructure/ServiceMigration.cs`
2. Implement bridge between DI container and ServiceLocator
3. Update ServiceLocator to delegate to DI container
4. Add logging for migration tracking

#### Code Template:
```csharp
// ServiceMigration.cs
namespace Maximagus.Infrastructure;

public static class ServiceMigration
{
    private static IServiceContainer? _container;
    
    public static void Initialize(IServiceContainer container)
    {
        _container = container;
        BridgeToServiceLocator();
    }
    
    private static void BridgeToServiceLocator()
    {
        // Bridge each service during migration
        ServiceLocator.RegisterFactory<ILogger>(() => _container.Resolve<ILogger>());
    }
}
```

#### Acceptance Criteria:
- [ ] ServiceLocator continues to work
- [ ] Services resolve from DI container
- [ ] Migration logging in place

---

## Phase 2: Domain Services (Weeks 2-3)

### Task 2.1: Create Spell Execution Service
**Priority**: Critical  
**Estimated Time**: 8 hours  
**Dependencies**: Phase 1 complete

#### Implementation Steps:
1. Create `Scripts/Services/Spell/ISpellExecutionService.cs`
2. Create `Scripts/Services/Spell/SpellExecutionService.cs`
3. Create `Scripts/Services/Spell/SpellExecutionContext.cs`
4. Create `Scripts/Services/Spell/SpellExecutionResult.cs`
5. Migrate logic from SpellLogicManager
6. Create comprehensive unit tests

#### Key Methods to Implement:
```csharp
public interface ISpellExecutionService
{
    SpellExecutionResult ExecuteSpell(SpellExecutionContext context);
    SpellValidationResult ValidateSpell(SpellExecutionContext context);
    SpellExecutionContext BuildContext(IGameStateData state, SpellCardResource spell);
}
```

#### Acceptance Criteria:
- [ ] Service handles all spell execution logic
- [ ] Clear separation from state management
- [ ] Unit tests cover all scenarios
- [ ] Performance metrics collected

---

### Task 2.2: Create Damage Calculation Service
**Priority**: High  
**Estimated Time**: 4 hours  
**Dependencies**: Task 2.1

#### Implementation Steps:
1. Create `Scripts/Services/Combat/IDamageCalculationService.cs`
2. Create `Scripts/Services/Combat/DamageCalculationService.cs`
3. Create `Scripts/Services/Combat/DamageResult.cs`
4. Extract damage logic from existing code
5. Add damage type support (physical, magical, etc.)
6. Create unit tests

#### Acceptance Criteria:
- [ ] All damage calculations centralized
- [ ] Support for different damage types
- [ ] Modifier system integrated
- [ ] Tests verify calculations

---

### Task 2.3: Create Modifier Service
**Priority**: High  
**Estimated Time**: 6 hours  
**Dependencies**: Task 2.1

#### Implementation Steps:
1. Create `Scripts/Services/Modifiers/IModifierService.cs`
2. Create `Scripts/Services/Modifiers/ModifierService.cs`
3. Create `Scripts/Services/Modifiers/ModifierCalculator.cs`
4. Implement modifier stacking rules
5. Add modifier categories
6. Create unit tests

#### Acceptance Criteria:
- [ ] Modifiers apply correctly
- [ ] Stacking rules enforced
- [ ] Performance optimized
- [ ] Edge cases tested

---

### Task 2.4: Create Status Effect Service
**Priority**: High  
**Estimated Time**: 6 hours  
**Dependencies**: Task 2.1

#### Implementation Steps:
1. Create `Scripts/Services/StatusEffects/IStatusEffectService.cs`
2. Create `Scripts/Services/StatusEffects/StatusEffectService.cs`
3. Create `Scripts/Services/StatusEffects/StatusEffectProcessor.cs`
4. Migrate logic from StatusEffectLogicManager
5. Remove static dependencies
6. Create unit tests

#### Acceptance Criteria:
- [ ] Service is fully injectable
- [ ] No static method calls
- [ ] All effects process correctly
- [ ] Tests cover all effect types

---

### Task 2.5: Consolidate Spell Commands
**Priority**: Critical  
**Estimated Time**: 10 hours  
**Dependencies**: Tasks 2.1-2.4

#### Implementation Steps:
1. Create `Scripts/Commands/Spell/ExecuteSpellCommand.cs`
2. Map existing command functionality to new command
3. Create command migration mapping document
4. Update command processor to route to new command
5. Mark old commands as deprecated
6. Update all command usages

#### Commands to Consolidate:
- StartSpellCommand
- PreCalculateSpellCommand
- ApplyEncounterSnapshotCommand
- ExecuteCardActionCommand
- AddSpellModifierCommand
- UpdateSpellPropertyCommand
- CompleteSpellCommand

#### Acceptance Criteria:
- [ ] Single command handles spell execution
- [ ] Old commands still work (deprecated)
- [ ] Performance maintained
- [ ] All tests pass

---

### Task 2.6: Implement Command Validation
**Priority**: Medium  
**Estimated Time**: 4 hours  
**Dependencies**: Task 2.5

#### Implementation Steps:
1. Create `Scripts/Commands/Validation/ValidationResult.cs`
2. Create `Scripts/Commands/Validation/ValidationError.cs`
3. Create `Scripts/Commands/Validation/ICommandValidator.cs`
4. Update all commands with validation
5. Add validation unit tests

#### Acceptance Criteria:
- [ ] All commands validate input
- [ ] Meaningful error messages
- [ ] Validation performance < 1ms
- [ ] Tests cover validation scenarios

---

## Phase 3: State Management (Weeks 4-6)

### Task 3.1: Implement State Builder Pattern
**Priority**: High  
**Estimated Time**: 6 hours  
**Dependencies**: Phase 2 complete

#### Implementation Steps:
1. Create `Scripts/State/Builders/GameStateBuilder.cs`
2. Create `Scripts/State/Builders/HandStateBuilder.cs`
3. Create `Scripts/State/Builders/PlayerStateBuilder.cs`
4. Create fluent API extensions
5. Migrate verbose state updates
6. Create builder tests

#### Code Template:
```csharp
public static class GameStateBuilder
{
    public static IGameStateData UpdateHand(this IGameStateData state, 
        Func<HandState, HandState> updater)
    {
        return state.WithHand(updater(state.Hand));
    }
}
```

#### Acceptance Criteria:
- [ ] Fluent API works correctly
- [ ] State immutability preserved
- [ ] Reduced code verbosity
- [ ] Tests verify builders

---

### Task 3.2: Segregate IGameState Interface
**Priority**: Critical  
**Estimated Time**: 8 hours  
**Dependencies**: Task 3.1

#### Implementation Steps:
1. Create focused interfaces:
   - `ICardStateProvider.cs`
   - `IPlayerStateProvider.cs`
   - `ISpellStateProvider.cs`
   - `IStatusEffectStateProvider.cs`
   - `IGamePhaseProvider.cs`
2. Update IGameState to compose interfaces
3. Create adapters for backward compatibility
4. Update all consumers gradually
5. Add interface tests

#### Acceptance Criteria:
- [ ] Interfaces properly segregated
- [ ] No breaking changes
- [ ] Consumers use minimal interfaces
- [ ] Tests verify interface contracts

---

### Task 3.3: Simplify Snapshot System
**Priority**: Medium  
**Estimated Time**: 10 hours  
**Dependencies**: Tasks 3.1, 3.2

#### Implementation Steps:
1. Create `Scripts/State/SpellExecutionContext.cs`
2. Replace triple snapshot with single context
3. Add real-time calculation option
4. Migrate existing snapshot usage
5. Performance test new system
6. Create migration tests

#### Acceptance Criteria:
- [ ] Single context replaces snapshots
- [ ] Performance maintained (<50ms)
- [ ] Memory usage reduced
- [ ] All features work correctly

---

### Task 3.4: Optimize State Updates
**Priority**: Medium  
**Estimated Time**: 6 hours  
**Dependencies**: Task 3.3

#### Implementation Steps:
1. Create `Scripts/State/Optimization/StateUpdateBatch.cs`
2. Implement batched update system
3. Add state diff tracking
4. Optimize hot paths
5. Add performance benchmarks
6. Create optimization tests

#### Acceptance Criteria:
- [ ] Batch updates work correctly
- [ ] GC pressure reduced by 50%
- [ ] Update time < 5ms
- [ ] Benchmarks show improvement

---

### Task 3.5: Remove ServiceLocator
**Priority**: High  
**Estimated Time**: 8 hours  
**Dependencies**: All previous tasks

#### Implementation Steps:
1. Audit all ServiceLocator usages
2. Replace with constructor injection
3. Update factory patterns
4. Remove ServiceLocator class
5. Update documentation
6. Final integration tests

#### Files to Update:
- All files using ServiceLocator.Instance
- Factory classes
- Static initialization code

#### Acceptance Criteria:
- [ ] ServiceLocator completely removed
- [ ] All dependencies injected
- [ ] No static service access
- [ ] All tests pass

---

## Testing & Validation Tasks

### Task T.1: Create Integration Test Suite
**Priority**: High  
**Estimated Time**: 8 hours  
**Dependencies**: Phase 2 complete

#### Implementation Steps:
1. Create `Tests/Integration/SpellSystemIntegrationTests.cs`
2. Create `Tests/Integration/StatusEffectIntegrationTests.cs`
3. Create `Tests/Integration/CommandIntegrationTests.cs`
4. Add end-to-end scenarios
5. Add performance benchmarks
6. Create test documentation

#### Acceptance Criteria:
- [ ] All systems integration tested
- [ ] Performance benchmarks included
- [ ] Edge cases covered
- [ ] Documentation complete

---

### Task T.2: Performance Profiling
**Priority**: Medium  
**Estimated Time**: 4 hours  
**Dependencies**: Phase 3 complete

#### Implementation Steps:
1. Set up profiling tools
2. Create performance test scenarios
3. Profile before/after refactoring
4. Document performance changes
5. Optimize hot paths
6. Create performance report

#### Acceptance Criteria:
- [ ] Performance metrics collected
- [ ] No performance regressions
- [ ] Hot paths optimized
- [ ] Report generated

---

### Task T.3: Code Coverage Analysis
**Priority**: Medium  
**Estimated Time**: 3 hours  
**Dependencies**: Task T.1

#### Implementation Steps:
1. Set up coverage tools
2. Run coverage analysis
3. Identify gaps
4. Add missing tests
5. Generate coverage report
6. Document coverage goals

#### Acceptance Criteria:
- [ ] Coverage > 80%
- [ ] Critical paths 100% covered
- [ ] Coverage report available
- [ ] Gaps documented

---

## Documentation Tasks

### Task D.1: Update Architecture Documentation
**Priority**: High  
**Estimated Time**: 4 hours  
**Dependencies**: Phase 3 complete

#### Implementation Steps:
1. Update `MemoryBank/ARCHITECTURE.md`
2. Create class diagrams
3. Document new patterns
4. Add migration guide
5. Update API documentation
6. Review and approve

#### Acceptance Criteria:
- [ ] Documentation current
- [ ] Diagrams updated
- [ ] Migration guide complete
- [ ] Reviewed and approved

---

### Task D.2: Create Developer Guide
**Priority**: Medium  
**Estimated Time**: 6 hours  
**Dependencies**: Task D.1

#### Implementation Steps:
1. Create `Documentation/DeveloperGuide.md`
2. Document coding standards
3. Explain new patterns
4. Add code examples
5. Include troubleshooting
6. Get team feedback

#### Acceptance Criteria:
- [ ] Guide comprehensive
- [ ] Examples working
- [ ] Team reviewed
- [ ] Feedback incorporated

---

## Risk Mitigation Tasks

### Task R.1: Create Feature Flags
**Priority**: High  
**Estimated Time**: 3 hours  
**Dependencies**: None

#### Implementation Steps:
1. Create `Scripts/Infrastructure/FeatureFlags.cs`
2. Add flags for major changes
3. Implement flag checking
4. Add configuration
5. Document flag usage
6. Test flag system

#### Acceptance Criteria:
- [ ] Flags control features
- [ ] Easy to toggle
- [ ] Well documented
- [ ] Tests verify flags

---

### Task R.2: Implement Rollback Plan
**Priority**: High  
**Estimated Time**: 2 hours  
**Dependencies**: Task R.1

#### Implementation Steps:
1. Document rollback procedures
2. Create rollback scripts
3. Test rollback process
4. Train team on rollback
5. Create rollback checklist
6. Review and approve

#### Acceptance Criteria:
- [ ] Rollback documented
- [ ] Scripts tested
- [ ] Team trained
- [ ] Checklist complete

---

## Success Metrics

### Code Quality Metrics
- [ ] Cyclomatic complexity < 10 for all methods
- [ ] Class coupling < 7 dependencies
- [ ] Class size < 200 lines
- [ ] Method size < 30 lines

### Performance Metrics
- [ ] Spell execution < 50ms
- [ ] State updates < 5ms
- [ ] Memory allocations reduced by 50%
- [ ] GC collections reduced by 30%

### Testing Metrics
- [ ] Unit test coverage > 80%
- [ ] Integration tests pass 100%
- [ ] Performance benchmarks met
- [ ] No regression in features

### Development Metrics
- [ ] PRs reviewed within 24 hours
- [ ] Build time < 30 seconds
- [ ] Test suite < 5 seconds
- [ ] Zero critical bugs

---

## Completion Checklist

### Phase 1 Complete
- [ ] DI container implemented
- [ ] All interfaces extracted
- [ ] Service configuration working
- [ ] SpellContextAdapter removed
- [ ] Migration bridge in place

### Phase 2 Complete
- [ ] All domain services created
- [ ] Commands consolidated
- [ ] Validation improved
- [ ] Static dependencies removed
- [ ] Services fully tested

### Phase 3 Complete
- [ ] State builders implemented
- [ ] Interfaces segregated
- [ ] Snapshots simplified
- [ ] Performance optimized
- [ ] ServiceLocator removed

### Project Complete
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Performance validated
- [ ] Team trained
- [ ] Metrics achieved

---

## Notes

- Tasks can be parallelized within phases
- Dependencies must be respected
- Each task should have a PR
- Code reviews required for all changes
- Performance testing after each phase
- Regular team sync meetings
- Document decisions and blockers