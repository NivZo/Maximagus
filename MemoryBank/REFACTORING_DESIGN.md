# Refactoring Design Document
## Spell, Status Effects, Commands & State Systems

### Document Version
- **Version**: 1.0
- **Date**: August 25, 2025
- **Author**: Technical Architecture Team
- **Status**: Draft for Implementation

---

## 1. Introduction

### 1.1 Purpose
This document provides the technical design for refactoring the Spell, Status Effects, Commands, and State systems in Maximagus. It translates the requirements from REFACTORING_REQUIREMENTS.md into concrete architectural patterns, interfaces, and implementation strategies.

### 1.2 Scope
The design covers:
- Dependency Injection infrastructure
- Domain service architecture
- Command pattern improvements
- State management simplification
- Interface segregation strategy

### 1.3 Design Principles
- **Incremental Refactoring**: Changes made in small, testable steps
- **Backward Compatibility**: Existing functionality preserved
- **SOLID Principles**: Strict adherence to all five principles
- **Testability First**: Design for testing from the start
- **Performance Awareness**: Monitor and maintain performance

---

## 2. Architecture Overview

### 2.1 Current Architecture Issues
```
┌─────────────────┐     Static      ┌──────────────────┐
│    Commands     │ ───────────────>│  ServiceLocator  │
└─────────────────┘                  └──────────────────┘
        │                                     │
        │ Executes                           │ Gets
        ↓                                     ↓
┌─────────────────┐                  ┌──────────────────┐
│   GameState     │←─────────────────│  Static Managers │
│   (500+ LOC)    │     Modifies     │  (Coupled)       │
└─────────────────┘                  └──────────────────┘
```

### 2.2 Target Architecture
```
┌─────────────────┐     DI Container    ┌──────────────────┐
│    Commands     │ <───────────────────│   Interfaces     │
└─────────────────┘                     └──────────────────┘
        │                                        │
        │ Uses                                   │ Implements
        ↓                                        ↓
┌─────────────────┐                     ┌──────────────────┐
│ Domain Services │                     │  Concrete Impl   │
└─────────────────┘                     └──────────────────┘
        │
        │ Returns
        ↓
┌─────────────────┐
│ Immutable State │
└─────────────────┘
```

---

## 3. Dependency Injection Design

### 3.1 DI Container Implementation

```csharp
public interface IServiceContainer
{
    void Register<TInterface, TImplementation>() 
        where TImplementation : TInterface;
    void RegisterSingleton<TInterface, TImplementation>() 
        where TImplementation : TInterface;
    void RegisterFactory<T>(Func<IServiceContainer, T> factory);
    T Resolve<T>();
    object Resolve(Type type);
}

public class ServiceContainer : IServiceContainer
{
    private readonly Dictionary<Type, ServiceDescriptor> _services = new();
    private readonly Dictionary<Type, object> _singletons = new();
    
    public void Register<TInterface, TImplementation>() 
        where TImplementation : TInterface
    {
        _services[typeof(TInterface)] = new ServiceDescriptor
        {
            ServiceType = typeof(TInterface),
            ImplementationType = typeof(TImplementation),
            Lifetime = ServiceLifetime.Transient
        };
    }
    
    public T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }
    
    public object Resolve(Type type)
    {
        if (_singletons.TryGetValue(type, out var singleton))
            return singleton;
            
        if (!_services.TryGetValue(type, out var descriptor))
            throw new InvalidOperationException($"Service {type.Name} not registered");
            
        var instance = CreateInstance(descriptor.ImplementationType);
        
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
            _singletons[type] = instance;
            
        return instance;
    }
    
    private object CreateInstance(Type type)
    {
        var constructor = type.GetConstructors().First();
        var parameters = constructor.GetParameters()
            .Select(p => Resolve(p.ParameterType))
            .ToArray();
        return Activator.CreateInstance(type, parameters);
    }
}
```

### 3.2 Service Registration

```csharp
public class ServiceConfiguration
{
    public static void ConfigureServices(IServiceContainer container)
    {
        // Core Services
        container.RegisterSingleton<ILogger, GodotLogger>();
        container.RegisterSingleton<IEventBus, SimpleEventBus>();
        container.RegisterSingleton<IGameCommandProcessor, GameCommandProcessor>();
        
        // Domain Services
        container.Register<ISpellExecutionService, SpellExecutionService>();
        container.Register<IDamageCalculationService, DamageCalculationService>();
        container.Register<IModifierService, ModifierService>();
        container.Register<IStatusEffectService, StatusEffectService>();
        
        // Managers
        container.Register<ISpellProcessingManager, SpellProcessingManager>();
        container.Register<IStatusEffectManager, StatusEffectManager>();
        container.Register<IHandManager, HandManager>();
        
        // Command Factories
        container.RegisterFactory<SpellCastCommand>(c => 
            new SpellCastCommand(
                c.Resolve<ISpellExecutionService>(),
                c.Resolve<ILogger>()
            ));
    }
}
```

---

## 4. Domain Service Architecture

### 4.1 Spell Execution Service

```csharp
public interface ISpellExecutionService
{
    SpellExecutionResult ExecuteSpell(SpellExecutionContext context);
    SpellValidationResult ValidateSpell(SpellExecutionContext context);
    SpellExecutionContext BuildContext(IGameStateData state, SpellCardResource spell);
}

public class SpellExecutionService : ISpellExecutionService
{
    private readonly IDamageCalculationService _damageCalc;
    private readonly IModifierService _modifierService;
    private readonly IStatusEffectService _statusEffectService;
    private readonly ILogger _logger;
    
    public SpellExecutionService(
        IDamageCalculationService damageCalc,
        IModifierService modifierService,
        IStatusEffectService statusEffectService,
        ILogger logger)
    {
        _damageCalc = damageCalc;
        _modifierService = modifierService;
        _statusEffectService = statusEffectService;
        _logger = logger;
    }
    
    public SpellExecutionResult ExecuteSpell(SpellExecutionContext context)
    {
        var result = new SpellExecutionResult();
        
        // Apply modifiers
        var modifiedContext = _modifierService.ApplyModifiers(context);
        
        // Calculate damage
        if (modifiedContext.HasDamageAction)
        {
            result.Damage = _damageCalc.Calculate(modifiedContext);
        }
        
        // Apply status effects
        if (modifiedContext.HasStatusEffects)
        {
            result.StatusEffects = _statusEffectService.Apply(modifiedContext);
        }
        
        result.FinalContext = modifiedContext;
        return result;
    }
    
    public SpellValidationResult ValidateSpell(SpellExecutionContext context)
    {
        var errors = new List<ValidationError>();
        
        if (context.Spell == null)
            errors.Add(new ValidationError("Spell cannot be null"));
            
        if (context.Caster == null)
            errors.Add(new ValidationError("Caster cannot be null"));
            
        // Additional validation...
        
        return new SpellValidationResult(errors);
    }
}
```

### 4.2 Damage Calculation Service

```csharp
public interface IDamageCalculationService
{
    DamageResult Calculate(SpellExecutionContext context);
    DamageResult CalculateModified(int baseDamage, IEnumerable<ModifierData> modifiers);
}

public class DamageCalculationService : IDamageCalculationService
{
    private readonly ILogger _logger;
    
    public DamageResult Calculate(SpellExecutionContext context)
    {
        var baseDamage = context.Spell.BaseDamage;
        var modifiers = context.Modifiers;
        
        // Apply additive modifiers
        var additiveBonus = modifiers
            .Where(m => m.Type == ModifierType.Additive)
            .Sum(m => m.Value);
            
        // Apply multiplicative modifiers
        var multiplier = modifiers
            .Where(m => m.Type == ModifierType.Multiplicative)
            .Aggregate(1.0f, (acc, m) => acc * (1 + m.Value));
            
        var finalDamage = (int)((baseDamage + additiveBonus) * multiplier);
        
        _logger.LogDebug($"Damage calculation: Base={baseDamage}, Final={finalDamage}");
        
        return new DamageResult
        {
            BaseDamage = baseDamage,
            FinalDamage = finalDamage,
            ModifiersApplied = modifiers.ToList()
        };
    }
}
```

### 4.3 Modifier Service

```csharp
public interface IModifierService
{
    SpellExecutionContext ApplyModifiers(SpellExecutionContext context);
    IEnumerable<ModifierData> GetActiveModifiers(IGameStateData state);
    ModifierData CreateModifier(ModifierActionResource resource);
}

public class ModifierService : IModifierService
{
    public SpellExecutionContext ApplyModifiers(SpellExecutionContext context)
    {
        var activeModifiers = GetActiveModifiers(context.GameState);
        
        return context.WithModifiers(
            context.Modifiers.Concat(activeModifiers)
        );
    }
    
    public IEnumerable<ModifierData> GetActiveModifiers(IGameStateData state)
    {
        // Get modifiers from status effects
        var statusModifiers = state.StatusEffects.ActiveEffects
            .SelectMany(e => e.Modifiers);
            
        // Get modifiers from equipment (future)
        // Get modifiers from buffs (future)
        
        return statusModifiers;
    }
}
```

---

## 5. Command Pattern Refactoring

### 5.1 Pure Command Implementation

```csharp
public abstract class PureGameCommand : GameCommand
{
    public sealed override CommandResult Execute(IGameStateData state, 
        CommandCompletionToken? token = null)
    {
        // Validate
        var validation = ValidateInternal(state);
        if (!validation.IsValid)
        {
            return CommandResult.Failure(validation.Errors.First());
        }
        
        // Execute pure transformation
        var executionResult = ExecutePure(state);
        
        // Log
        LogExecution(state, executionResult);
        
        return executionResult;
    }
    
    protected abstract CommandResult ExecutePure(IGameStateData state);
    protected abstract ValidationResult ValidateInternal(IGameStateData state);
    
    private void LogExecution(IGameStateData state, CommandResult result)
    {
        // Centralized logging
    }
}
```

### 5.2 Command Result Enhancement

```csharp
public class CommandResult
{
    public bool Success { get; init; }
    public IGameStateData? NewState { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<GameCommand> FollowUpCommands { get; init; } = Array.Empty<GameCommand>();
    public IReadOnlyList<IGameEvent> Events { get; init; } = Array.Empty<IGameEvent>();
    
    public static CommandResult SuccessWithState(IGameStateData newState)
    {
        return new CommandResult 
        { 
            Success = true, 
            NewState = newState 
        };
    }
    
    public static CommandResult SuccessWithFollowUp(
        IGameStateData newState, 
        params GameCommand[] followUpCommands)
    {
        return new CommandResult 
        { 
            Success = true, 
            NewState = newState,
            FollowUpCommands = followUpCommands
        };
    }
    
    public static CommandResult FailureWithDetails(ValidationResult validation)
    {
        return new CommandResult 
        { 
            Success = false, 
            ErrorMessage = validation.ToString()
        };
    }
}
```

### 5.3 Consolidated Spell Command

```csharp
public class ExecuteSpellCommand : PureGameCommand
{
    private readonly ISpellExecutionService _spellService;
    private readonly SpellCardResource _spell;
    
    public ExecuteSpellCommand(
        ISpellExecutionService spellService,
        SpellCardResource spell)
    {
        _spellService = spellService;
        _spell = spell;
    }
    
    protected override CommandResult ExecutePure(IGameStateData state)
    {
        // Build context
        var context = _spellService.BuildContext(state, _spell);
        
        // Execute spell
        var result = _spellService.ExecuteSpell(context);
        
        // Apply results to state
        var newState = ApplySpellResults(state, result);
        
        // Create follow-up commands
        var followUps = new List<GameCommand>();
        
        if (result.StatusEffects.Any())
        {
            followUps.Add(new UpdateStatusEffectsCommand(result.StatusEffects));
        }
        
        if (result.Damage != null)
        {
            followUps.Add(new ApplyDamageCommand(result.Damage));
        }
        
        return CommandResult.SuccessWithFollowUp(newState, followUps.ToArray());
    }
    
    protected override ValidationResult ValidateInternal(IGameStateData state)
    {
        var context = _spellService.BuildContext(state, _spell);
        return _spellService.ValidateSpell(context).ToValidationResult();
    }
}
```

---

## 6. State Management Improvements

### 6.1 State Builder Pattern

```csharp
public static class GameStateBuilder
{
    public static IGameStateData UpdateHand(
        this IGameStateData state, 
        Func<HandState, HandState> updater)
    {
        return state.WithHand(updater(state.Hand));
    }
    
    public static IGameStateData UpdatePlayer(
        this IGameStateData state,
        Func<PlayerState, PlayerState> updater)
    {
        return state.WithPlayer(updater(state.Player));
    }
    
    public static IGameStateData UpdateSpell(
        this IGameStateData state,
        Func<SpellState, SpellState> updater)
    {
        return state.WithSpell(updater(state.Spell));
    }
    
    // Chain multiple updates
    public static IGameStateData ApplyUpdates(
        this IGameStateData state,
        params Func<IGameStateData, IGameStateData>[] updates)
    {
        return updates.Aggregate(state, (current, update) => update(current));
    }
}

// Usage example:
var newState = state
    .UpdateHand(h => h.WithCards(newCards))
    .UpdatePlayer(p => p.WithMana(p.Mana - cost))
    .UpdateSpell(s => s.WithActive(true));
```

### 6.2 Interface Segregation

```csharp
// Split the monolithic IGameState interface
public interface ICardStateProvider
{
    HandState Hand { get; }
    IReadOnlyList<CardState> AllCards { get; }
}

public interface IPlayerStateProvider
{
    PlayerState Player { get; }
    int CurrentMana { get; }
    int MaxMana { get; }
}

public interface ISpellStateProvider
{
    SpellState Spell { get; }
    bool IsSpellActive { get; }
}

public interface IStatusEffectStateProvider
{
    StatusEffectsState StatusEffects { get; }
}

public interface IGamePhaseProvider
{
    GamePhaseState Phase { get; }
}

// Main interface composes focused interfaces
public interface IGameStateData : 
    ICardStateProvider,
    IPlayerStateProvider,
    ISpellStateProvider,
    IStatusEffectStateProvider,
    IGamePhaseProvider
{
    // Only truly global properties here
    int TurnNumber { get; }
    DateTime Timestamp { get; }
}
```

### 6.3 Simplified Spell Context

```csharp
public class SpellExecutionContext
{
    public SpellCardResource Spell { get; init; }
    public IGameStateData GameState { get; init; }
    public IReadOnlyList<ModifierData> Modifiers { get; init; } = Array.Empty<ModifierData>();
    public IReadOnlyList<StatusEffectInstanceData> ActiveEffects { get; init; } = Array.Empty<StatusEffectInstanceData>();
    
    // Builder methods for immutability
    public SpellExecutionContext WithModifiers(IEnumerable<ModifierData> modifiers)
    {
        return this with { Modifiers = modifiers.ToList() };
    }
    
    public SpellExecutionContext WithEffects(IEnumerable<StatusEffectInstanceData> effects)
    {
        return this with { ActiveEffects = effects.ToList() };
    }
    
    // Computed properties
    public bool HasDamageAction => Spell.Actions.Any(a => a is DamageActionResource);
    public bool HasStatusEffects => Spell.Actions.Any(a => a is StatusEffectActionResource);
    public int TotalDamageModifier => Modifiers.Where(m => m.Type == ModifierType.Damage).Sum(m => m.Value);
}
```

---

## 7. Testing Infrastructure

### 7.1 Test Builders

```csharp
public class GameStateTestBuilder
{
    private HandState _hand = HandState.Empty;
    private PlayerState _player = PlayerState.Default;
    private SpellState _spell = SpellState.Inactive;
    
    public GameStateTestBuilder WithHand(params CardState[] cards)
    {
        _hand = new HandState { Cards = cards.ToList() };
        return this;
    }
    
    public GameStateTestBuilder WithPlayer(int mana, int health)
    {
        _player = new PlayerState { Mana = mana, Health = health };
        return this;
    }
    
    public GameStateTestBuilder WithActiveSpell(SpellCardResource spell)
    {
        _spell = new SpellState { ActiveSpell = spell, IsActive = true };
        return this;
    }
    
    public IGameStateData Build()
    {
        return new GameState
        {
            Hand = _hand,
            Player = _player,
            Spell = _spell,
            Phase = GamePhaseState.Default,
            StatusEffects = StatusEffectsState.Empty
        };
    }
}
```

### 7.2 Mock Services

```csharp
public class MockSpellExecutionService : ISpellExecutionService
{
    private readonly Queue<SpellExecutionResult> _results = new();
    
    public void SetupResult(SpellExecutionResult result)
    {
        _results.Enqueue(result);
    }
    
    public SpellExecutionResult ExecuteSpell(SpellExecutionContext context)
    {
        if (_results.Count == 0)
            throw new InvalidOperationException("No mock result configured");
            
        return _results.Dequeue();
    }
    
    public SpellValidationResult ValidateSpell(SpellExecutionContext context)
    {
        return SpellValidationResult.Success;
    }
}
```

---

## 8. Migration Strategy

### 8.1 Phase 1: Foundation (Week 1)

```csharp
// Step 1: Create DI container alongside ServiceLocator
public class ServiceMigration
{
    private static IServiceContainer? _container;
    
    public static void Initialize()
    {
        _container = new ServiceContainer();
        ServiceConfiguration.ConfigureServices(_container);
        
        // Bridge to ServiceLocator during migration
        ServiceLocator.RegisterFactory<ILogger>(() => _container.Resolve<ILogger>());
    }
    
    public static T Resolve<T>() => _container!.Resolve<T>();
}

// Step 2: Extract interfaces for existing managers
public interface ISpellLogicManager
{
    SpellContext CalculateSpellContext(IGameStateData state, SpellCardResource spell);
    int CalculateDamage(SpellContext context);
}

// Step 3: Update managers to implement interfaces
public class SpellLogicManager : ISpellLogicManager
{
    // Existing implementation
}
```

### 8.2 Phase 2: Service Layer (Weeks 2-3)

```csharp
// Step 1: Create domain services that use existing managers
public class SpellExecutionService : ISpellExecutionService
{
    private readonly ISpellLogicManager _spellLogic;
    
    public SpellExecutionService(ISpellLogicManager spellLogic)
    {
        _spellLogic = spellLogic;
    }
    
    public SpellExecutionResult ExecuteSpell(SpellExecutionContext context)
    {
        // Delegate to existing manager initially
        var oldContext = _spellLogic.CalculateSpellContext(
            context.GameState, 
            context.Spell);
            
        // Gradually move logic here
        return ConvertToNewResult(oldContext);
    }
}

// Step 2: Update commands to use services
public class SpellCastCommand : GameCommand
{
    private readonly ISpellExecutionService _spellService;
    
    public SpellCastCommand(ISpellExecutionService spellService)
    {
        _spellService = spellService ?? 
            ServiceMigration.Resolve<ISpellExecutionService>();
    }
}
```

### 8.3 Phase 3: State Refactoring (Weeks 4-6)

```csharp
// Step 1: Add builder extensions without changing core state
public static class StateBuilderExtensions
{
    // Extension methods as shown in section 6.1
}

// Step 2: Gradually replace verbose state updates
// Before:
var newHand = state.Hand.WithCards(updatedCards);
var newState = state.WithHand(newHand);

// After:
var newState = state.UpdateHand(h => h.WithCards(updatedCards));

// Step 3: Implement interface segregation with adapters
public class GameStateAdapter : ICardStateProvider, IPlayerStateProvider
{
    private readonly GameState _state;
    
    public GameStateAdapter(GameState state) => _state = state;
    
    public HandState Hand => _state.Hand;
    public PlayerState Player => _state.Player;
}
```

---

## 9. Performance Considerations

### 9.1 Object Pooling

```csharp
public class CommandPool<T> where T : GameCommand, new()
{
    private readonly Stack<T> _pool = new();
    private readonly int _maxSize;
    
    public CommandPool(int maxSize = 100)
    {
        _maxSize = maxSize;
    }
    
    public T Rent()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }
    
    public void Return(T command)
    {
        command.Reset();
        if (_pool.Count < _maxSize)
            _pool.Push(command);
    }
}
```

### 9.2 State Update Optimization

```csharp
public class StateUpdateBatch
{
    private readonly List<Func<IGameStateData, IGameStateData>> _updates = new();
    
    public void Add(Func<IGameStateData, IGameStateData> update)
    {
        _updates.Add(update);
    }
    
    public IGameStateData Apply(IGameStateData state)
    {
        // Apply all updates in single pass
        return _updates.Aggregate(state, (current, update) => update(current));
    }
}
```

---

## 10. Monitoring & Metrics

### 10.1 Performance Monitoring

```csharp
public class PerformanceMonitor
{
    private readonly Dictionary<string, List<long>> _metrics = new();
    
    public IDisposable MeasureOperation(string operationName)
    {
        return new OperationTimer(this, operationName);
    }
    
    private class OperationTimer : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operation;
        private readonly Stopwatch _stopwatch;
        
        public OperationTimer(PerformanceMonitor monitor, string operation)
        {
            _monitor = monitor;
            _operation = operation;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operation, _stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### 10.2 Code Metrics Collection

```csharp
public class CodeMetrics
{
    public static ComplexityReport AnalyzeComplexity(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var complexities = methods.Select(m => CalculateCyclomaticComplexity(m));
        
        return new ComplexityReport
        {
            ClassName = type.Name,
            MaxComplexity = complexities.Max(),
            AverageComplexity = complexities.Average(),
            MethodCount = methods.Length
        };
    }
}
```

---

## Appendix A: Interface Definitions

[Complete interface definitions for all services and components]

## Appendix B: Configuration Examples

[Sample configuration files and setup code]

## Appendix C: Migration Checklist

[Detailed checklist for each migration phase]