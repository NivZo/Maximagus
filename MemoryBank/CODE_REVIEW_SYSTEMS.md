# Technical Code Review: Spell Casting, Status Effects, Commands, and State Systems

## Executive Summary

This document presents a technical code review of the interconnected systems responsible for spell casting, status effects, command processing, and state management in the Maximagus project. The review focuses on architecture cleanliness, SOLID principles adherence, and opportunities for simplification.

## Overall Architecture Assessment

### Strengths
1. **Immutable State Pattern**: The use of immutable state objects (`GameState`, `SpellState`, `StatusEffectsState`) provides excellent predictability and thread safety.
2. **Command Pattern Implementation**: Well-structured command system with clear separation of concerns.
3. **Snapshot-Based Pre-calculation**: The `EncounterStateSnapshot` system provides deterministic spell execution with preview capabilities.
4. **Pure Functions in Logic Managers**: Static methods in `SpellLogicManager` and `StatusEffectLogicManager` avoid side effects.

### Key Issues Identified
1. **Circular Dependencies**: Complex interdependencies between Commands, State, and Logic Managers.
2. **Mixed Responsibilities**: Some classes violate Single Responsibility Principle.
3. **Tight Coupling**: Direct references to concrete implementations rather than interfaces.
4. **Inconsistent Abstraction Levels**: Business logic scattered across multiple layers.

## System-by-System Analysis

### 1. State System

#### Current Implementation
- **IGameState** interface with multiple implementations (`GameState`, `EncounterState`)
- Immutable state objects with "With" methods for state transitions
- Complex state composition with nested state objects

#### SOLID Violations
1. **Interface Segregation Principle (ISP)**: `IGameState` is too broad, forcing implementations to support all aspects of game state.
2. **Single Responsibility Principle (SRP)**: `GameState` manages too many concerns (cards, phase, player, spell, status effects).

#### Recommendations
```csharp
// Split IGameState into focused interfaces
public interface ICardState
{
    CardsState Cards { get; }
    ICardState WithCards(CardsState cards);
}

public interface ISpellState
{
    SpellState Spell { get; }
    ISpellState WithSpell(SpellState spell);
}

public interface IStatusEffectState
{
    StatusEffectsState StatusEffects { get; }
    IStatusEffectState WithStatusEffects(StatusEffectsState effects);
}

// GameState implements all interfaces
public class GameState : ICardState, ISpellState, IStatusEffectState, IGameStateData
{
    // Current implementation, but with explicit interface implementation
}
```

### 2. Command System

#### Current Implementation
- Abstract `GameCommand` base class
- Concrete command classes for each action
- `GameCommandProcessor` orchestrating execution
- `CommandCompletionToken` for async completion

#### SOLID Violations
1. **Dependency Inversion Principle (DIP)**: Commands directly reference concrete managers instead of interfaces.
2. **Open/Closed Principle (OCP)**: Adding new command types requires modifying multiple places.

#### Code Smells
- **Long Parameter Lists**: Some commands require multiple parameters that could be grouped.
- **Duplicate Logic**: Similar validation logic repeated across commands.

#### Recommendations
```csharp
// Introduce command context to reduce parameter passing
public class CommandContext
{
    public IGameStateData CurrentState { get; }
    public ILogger Logger { get; }
    public IEventBus EventBus { get; }
    // Other shared dependencies
}

// Simplify command interface
public abstract class GameCommand
{
    protected CommandContext Context { get; private set; }
    
    public void Initialize(CommandContext context)
    {
        Context = context;
    }
    
    public abstract bool CanExecute();
    public abstract void Execute(CommandCompletionToken token);
}

// Extract validation logic into validators
public interface ICommandValidator<TCommand> where TCommand : GameCommand
{
    bool Validate(TCommand command, IGameStateData state);
}
```

### 3. Spell System

#### Current Implementation
- `SpellProcessingManager` orchestrating spell execution
- `SpellLogicManager` with static methods for business logic
- `EncounterStateSnapshot` for pre-calculation
- Complex modifier system with stacking and consumption

#### Issues
1. **God Object**: `SpellLogicManager` has too many responsibilities (482 lines).
2. **Feature Envy**: Logic managers constantly accessing state object internals.
3. **Procedural Code**: Static methods prevent proper dependency injection and testing.

#### Recommendations
```csharp
// Convert static managers to services with interfaces
public interface ISpellProcessor
{
    EncounterStateSnapshot PreCalculateAction(ActionResource action, EncounterState state);
    ImmutableArray<EncounterStateSnapshot> PreCalculateSpell(IGameStateData initialState, IEnumerable<CardState> cards);
    IGameStateData ApplySnapshot(IGameStateData state, EncounterStateSnapshot snapshot);
}

public class SpellProcessor : ISpellProcessor
{
    private readonly IModifierCalculator _modifierCalculator;
    private readonly IDamageCalculator _damageCalculator;
    private readonly ILogger _logger;
    
    public SpellProcessor(IModifierCalculator modifierCalculator, 
                         IDamageCalculator damageCalculator,
                         ILogger logger)
    {
        _modifierCalculator = modifierCalculator;
        _damageCalculator = damageCalculator;
        _logger = logger;
    }
    
    // Implementation with proper dependency injection
}

// Extract specific responsibilities
public interface IModifierCalculator
{
    (float finalDamage, ImmutableArray<ModifierData> consumed) Calculate(
        DamageActionResource action, 
        ImmutableArray<ModifierData> modifiers);
}

public interface IDamageCalculator
{
    float CalculateBaseDamage(DamageActionResource action, IGameStateData state);
}
```

### 4. Status Effect System

#### Current Implementation
- `StatusEffectResource` as ScriptableObject pattern
- `StatusEffectInstanceData` for runtime instances
- `StatusEffectLogicManager` with static methods
- Trigger and decay systems

#### Issues
1. **Tight Coupling**: `StatusEffectResource.OnTrigger()` creates side effects directly.
2. **Mixed Concerns**: Resource definitions contain runtime logic.
3. **Limited Extensibility**: Hard to add new effect types without modifying core classes.

#### Recommendations
```csharp
// Separate effect definition from behavior
public interface IStatusEffectBehavior
{
    void OnApply(StatusEffectContext context);
    void OnTrigger(StatusEffectContext context);
    void OnRemove(StatusEffectContext context);
}

public class StatusEffectContext
{
    public StatusEffectInstanceData Instance { get; }
    public IGameStateData GameState { get; }
    public IEventBus EventBus { get; }
}

// Factory pattern for effect behaviors
public interface IStatusEffectFactory
{
    IStatusEffectBehavior CreateBehavior(StatusEffectType type);
}

// Registry pattern for extensibility
public class StatusEffectRegistry
{
    private readonly Dictionary<StatusEffectType, Func<IStatusEffectBehavior>> _factories;
    
    public void Register(StatusEffectType type, Func<IStatusEffectBehavior> factory)
    {
        _factories[type] = factory;
    }
    
    public IStatusEffectBehavior CreateBehavior(StatusEffectType type)
    {
        return _factories.TryGetValue(type, out var factory) 
            ? factory() 
            : new NullStatusEffectBehavior();
    }
}
```

## Critical Improvements Needed

### 1. Dependency Management
**Problem**: Circular dependencies and ServiceLocator anti-pattern usage.

**Solution**:
```csharp
// Use proper dependency injection
public class CommandProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public IGameCommandProcessor Create()
    {
        return new GameCommandProcessor(
            _serviceProvider.GetService<ILogger>(),
            _serviceProvider.GetService<IEventBus>(),
            _serviceProvider.GetService<ISpellProcessor>(),
            _serviceProvider.GetService<IStatusEffectManager>()
        );
    }
}
```

### 2. State Mutation Clarity
**Problem**: Complex state transitions are hard to track and debug.

**Solution**:
```csharp
// Introduce state transition objects
public class StateTransition
{
    public IGameStateData FromState { get; }
    public IGameStateData ToState { get; }
    public string Reason { get; }
    public DateTime Timestamp { get; }
    
    public StateTransition(IGameStateData from, IGameStateData to, string reason)
    {
        FromState = from;
        ToState = to;
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }
}

// Track state changes
public interface IStateHistory
{
    void RecordTransition(StateTransition transition);
    IEnumerable<StateTransition> GetHistory();
    void Clear();
}
```

### 3. Business Logic Organization
**Problem**: Business logic scattered across commands, managers, and state objects.

**Solution**:
```csharp
// Domain service pattern
public interface ISpellDomainService
{
    SpellValidationResult ValidateSpellCast(IGameStateData state, SpellCardResource spell);
    SpellExecutionPlan CreateExecutionPlan(IGameStateData state, IEnumerable<CardState> cards);
    IGameStateData ExecutePlan(IGameStateData state, SpellExecutionPlan plan);
}

public class SpellExecutionPlan
{
    public ImmutableArray<PlannedAction> Actions { get; }
    public ImmutableArray<EncounterStateSnapshot> Snapshots { get; }
    public float TotalDamage { get; }
    public TimeSpan EstimatedDuration { get; }
}
```

### 4. Simplify EncounterState/GameState Duality
**Problem**: Two overlapping state representations cause confusion.

**Solution**:
```csharp
// Single state with view projections
public interface IStateProjection<T>
{
    T Project(IGameStateData state);
}

public class EncounterProjection : IStateProjection<EncounterView>
{
    public EncounterView Project(IGameStateData state)
    {
        return new EncounterView
        {
            StatusEffects = state.StatusEffects,
            SpellModifiers = state.Spell.ActiveModifiers,
            CurrentPhase = state.Phase.CurrentPhase
        };
    }
}
```

## Performance Considerations

1. **Immutable State Overhead**: Consider object pooling for frequently created state objects.
2. **Snapshot Memory Usage**: Implement snapshot compression or limit history depth.
3. **Static Method Calls**: Convert to instance methods for better caching opportunities.

## Testing Improvements

1. **Introduce Test Builders**:
```csharp
public class GameStateBuilder
{
    private CardsState _cards = CardsState.CreateInitial();
    private SpellState _spell = SpellState.CreateInitial();
    
    public GameStateBuilder WithCards(Action<CardsStateBuilder> configure)
    {
        var builder = new CardsStateBuilder();
        configure(builder);
        _cards = builder.Build();
        return this;
    }
    
    public IGameStateData Build() => new GameState(_cards, _spell, ...);
}
```

2. **Mock Simplification**: Create interface-based mocks instead of concrete class mocks.

## Migration Strategy

### Phase 1: Interface Extraction (Low Risk)
1. Extract interfaces from concrete managers
2. Add dependency injection infrastructure
3. Update commands to use interfaces

### Phase 2: Logic Consolidation (Medium Risk)
1. Move business logic from static classes to services
2. Implement domain service pattern
3. Simplify state transition logic

### Phase 3: State System Refactoring (High Risk)
1. Unify EncounterState and GameState
2. Implement state projections
3. Optimize immutable operations

## Conclusion

The current implementation shows good architectural foundations with the command pattern and immutable state, but suffers from violations of SOLID principles that create maintenance challenges. The recommended improvements focus on:

1. **Separation of Concerns**: Breaking down large classes and extracting focused interfaces
2. **Dependency Inversion**: Using interfaces instead of concrete implementations
3. **Single Responsibility**: Ensuring each class has one reason to change
4. **Testability**: Making the code easier to unit test through proper dependency injection

These changes would significantly improve code maintainability, testability, and extensibility while preserving the current functionality.

## Priority Actions

1. **Immediate** (Can be done now):
   - Extract interfaces from SpellLogicManager and StatusEffectLogicManager
   - Remove ServiceLocator usage in favor of constructor injection
   
2. **Short-term** (Next sprint):
   - Implement command validators to reduce duplication
   - Create domain services for spell and status effect logic
   
3. **Long-term** (Future refactoring):
   - Unify state management system
   - Implement proper event sourcing if state history is important
   - Consider CQRS pattern for complex state queries