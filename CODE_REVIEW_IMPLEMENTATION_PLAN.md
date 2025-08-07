# Maximagus - Code Review Implementation Plan

## High-Level Refactoring Strategy

### Phase 1: Foundation Fixes (Critical - 2-3 weeks)
**Goal**: Fix architectural violations that prevent further development
1. **Dependency Injection Implementation** - Replace ServiceLocator anti-pattern
2. **Command Pattern Restoration** - Fix command violations and circular dependencies
3. **Interface Standardization** - Resolve naming and design inconsistencies
4. **State Validation Enhancement** - Ensure robust state management

### Phase 2: Extensibility Improvements (High Priority - 3-4 weeks)  
**Goal**: Remove hard-coded limitations and enable future feature development
1. **Game Phase System Redesign** - Make phases configurable and extensible
2. **Status Effect System Overhaul** - Enable runtime effect creation and composition
3. **Action System Enhancement** - Add composition and chaining capabilities
4. **Event System Standardization** - Create consistent event patterns

### Phase 3: Performance & Polish (Medium Priority - 2-3 weeks)
**Goal**: Optimize performance and improve developer experience
1. **Performance Optimization** - Address LINQ and object creation issues
2. **Testing Infrastructure** - Add comprehensive unit test coverage
3. **Development Tools** - Create debugging and analysis utilities
4. **Documentation Updates** - Synchronize code with documentation

---

## Detailed Implementation Plan

## Phase 1: Foundation Fixes

### 1.1 Dependency Injection Implementation (Week 1)

#### **Current Problem:**
```csharp
public GameCommand()
{
    _logger = ServiceLocator.GetService<ILogger>();
    _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
}
```

#### **Solution: Container-Based DI**

**Step 1: Create DI Container Interface**
```csharp
// Scripts/Interfaces/Infra/IDependencyContainer.cs
public interface IDependencyContainer
{
    void RegisterSingleton<TInterface, TImplementation>() 
        where TImplementation : class, TInterface;
    void RegisterTransient<TInterface, TImplementation>() 
        where TImplementation : class, TInterface;
    void RegisterInstance<T>(T instance);
    T Resolve<T>();
    void BuildContainer();
}
```

**Step 2: Implement Lightweight DI Container**
```csharp
// Scripts/Implementations/Infra/DependencyContainer.cs
public class DependencyContainer : IDependencyContainer
{
    private readonly Dictionary<Type, ServiceDescriptor> _services = new();
    private readonly Dictionary<Type, object> _singletonInstances = new();
    
    // Implementation with proper lifetime management
}
```

**Step 3: Refactor GameCommand Base Class**
```csharp
// Scripts/Commands/GameCommand.cs
public abstract class GameCommand
{
    protected readonly ILogger Logger;
    protected readonly IGameStateData GameState;

    protected GameCommand(ILogger logger, IGameStateData gameState)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
    }

    public abstract bool CanExecute();
    public abstract IGameStateData Execute();
    public abstract string GetDescription();
}
```

**Step 4: Update ServiceLocator**
```csharp
// Scripts/Implementations/Infra/ServiceLocator.cs (Updated)
public static class ServiceLocator
{
    private static IDependencyContainer _container;
    
    public static void Initialize(IDependencyContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }
    
    public static T GetService<T>() => _container.Resolve<T>();
}
```

### 1.2 Command Pattern Restoration (Week 1-2)

#### **Current Problem:**
```csharp
// PlayHandCommand.Execute() - VIOLATION
public override IGameStateData Execute()
{
    _commandProcessor.SetState(newState);  // ❌ Commands modifying processor
    _spellProcessingManager.ProcessSpell(); // ❌ Side effects in command
    _commandProcessor.ExecuteCommand(command); // ❌ Commands executing commands
    return _commandProcessor.CurrentState; // ❌ Not returning computed state
}
```

#### **Solution: Pure Command Implementation**

**Step 1: Create Command Result Pattern**
```csharp
// Scripts/Commands/CommandResult.cs
public class CommandResult
{
    public IGameStateData NewState { get; }
    public IEnumerable<GameCommand> FollowUpCommands { get; }
    public IEnumerable<IGameEvent> Events { get; }
    public bool Success { get; }
    public string ErrorMessage { get; }

    private CommandResult(IGameStateData newState, 
                         IEnumerable<GameCommand> followUpCommands = null,
                         IEnumerable<IGameEvent> events = null,
                         bool success = true,
                         string errorMessage = null)
    {
        NewState = newState;
        FollowUpCommands = followUpCommands ?? Enumerable.Empty<GameCommand>();
        Events = events ?? Enumerable.Empty<IGameEvent>();
        Success = success;
        ErrorMessage = errorMessage;
    }

    public static CommandResult Success(IGameStateData newState, 
                                      IEnumerable<GameCommand> followUpCommands = null,
                                      IEnumerable<IGameEvent> events = null)
        => new(newState, followUpCommands, events);

    public static CommandResult Failure(string errorMessage)
        => new(null, success: false, errorMessage: errorMessage);
}
```

**Step 2: Refactor Command Interface**
```csharp
// Scripts/Commands/IGameCommand.cs (Renamed from GameCommand)
public interface IGameCommand
{
    bool CanExecute(IGameStateData currentState);
    CommandResult Execute(IGameStateData currentState);
    string GetDescription();
}
```

**Step 3: Implement Pure PlayHandCommand**
```csharp
// Scripts/Commands/Hand/PlayHandCommand.cs (Refactored)
public class PlayHandCommand : IGameCommand
{
    private readonly ISpellProcessingManager _spellProcessor;
    private readonly ILogger _logger;

    public PlayHandCommand(ISpellProcessingManager spellProcessor, ILogger logger)
    {
        _spellProcessor = spellProcessor;
        _logger = logger;
    }

    public bool CanExecute(IGameStateData currentState)
    {
        return currentState?.Phase?.AllowsCardSelection == true &&
               currentState?.Player?.HasHandsRemaining == true &&
               currentState?.Hand?.SelectedCount > 0 &&
               !currentState.Hand.IsLocked;
    }

    public CommandResult Execute(IGameStateData currentState)
    {
        _logger.LogInfo("[PlayHandCommand] Processing spell execution");

        // Pure computation - no side effects
        var spellResult = _spellProcessor.ProcessSpell(currentState.Hand.SelectedCards);
        
        var newPlayerState = currentState.Player.WithHandUsed();
        var newHandState = currentState.Hand.WithRemovedCards(
            currentState.Hand.SelectedCards.Select(c => c.CardId));
        var newPhaseState = currentState.Phase.WithPhase(GamePhase.TurnEnd);
        
        var newState = currentState
            .WithPlayer(newPlayerState)
            .WithHand(newHandState)
            .WithPhase(newPhaseState);

        // Follow-up commands for next phase
        var followUpCommands = new[] { new TurnStartCommand() };
        
        // Events for UI updates
        var events = new IGameEvent[] 
        {
            new SpellCastEvent(spellResult),
            new HandUpdatedEvent(newHandState),
            new PhaseChangedEvent(newPhaseState)
        };

        return CommandResult.Success(newState, followUpCommands, events);
    }

    public string GetDescription() => "Play selected cards as spell";
}
```

**Step 4: Update GameCommandProcessor**
```csharp
// Scripts/Commands/GameCommandProcessor.cs (Enhanced)
public class GameCommandProcessor : IGameCommandProcessor
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private IGameStateData _currentState;
    private readonly Queue<IGameCommand> _commandQueue = new();

    public bool ExecuteCommand(IGameCommand command)
    {
        if (!command.CanExecute(_currentState))
        {
            _logger.LogWarning($"Command rejected: {command.GetDescription()}");
            return false;
        }

        var result = command.Execute(_currentState);
        if (!result.Success)
        {
            _logger.LogError($"Command failed: {result.ErrorMessage}");
            return false;
        }

        // Update state
        var previousState = _currentState;
        _currentState = result.NewState;

        // Publish events
        foreach (var gameEvent in result.Events)
        {
            _eventBus.Publish(gameEvent);
        }

        // Queue follow-up commands
        foreach (var followUpCommand in result.FollowUpCommands)
        {
            _commandQueue.Enqueue(followUpCommand);
        }

        // Process queued commands
        ProcessQueuedCommands();

        return true;
    }

    private void ProcessQueuedCommands()
    {
        while (_commandQueue.Count > 0)
        {
            var command = _commandQueue.Dequeue();
            ExecuteCommand(command);
        }
    }
}
```

### 1.3 Interface Standardization (Week 2)

#### **Fix Interface Naming and Design**

**Step 1: Rename and Clarify Interfaces**
```csharp
// Scripts/Commands/IGameCommand.cs → Scripts/Commands/ICommand.cs
public interface ICommand
{
    bool CanExecute(IGameStateData currentState);
    CommandResult Execute(IGameStateData currentState);
    string GetDescription();
}

// Scripts/Commands/GameCommand.cs → Scripts/Commands/BaseCommand.cs
public abstract class BaseCommand : ICommand
{
    protected readonly ILogger Logger;
    
    protected BaseCommand(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public abstract bool CanExecute(IGameStateData currentState);
    public abstract CommandResult Execute(IGameStateData currentState);
    public abstract string GetDescription();
}
```

**Step 2: Standardize Event Interfaces**
```csharp
// Scripts/Events/IGameEvent.cs
public interface IGameEvent
{
    DateTime Timestamp { get; }
    string EventType { get; }
    object EventData { get; }
}

// Scripts/Events/BaseGameEvent.cs
public abstract class BaseGameEvent : IGameEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public abstract object EventData { get; }
}

// Scripts/Events/CardEvents.cs (Refactored)
public class CardDragStartedEvent : BaseGameEvent
{
    public override string EventType => "CardDragStarted";
    public override object EventData => new { Card };
    
    public Card Card { get; }
    
    public CardDragStartedEvent(Card card)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
    }
}
```

### 1.4 State Validation Enhancement (Week 2-3)

#### **Implement Comprehensive Validation**

**Step 1: Create Validation Framework**
```csharp
// Scripts/State/Validation/IStateValidator.cs
public interface IStateValidator<T>
{
    ValidationResult Validate(T state);
}

// Scripts/State/Validation/ValidationResult.cs
public class ValidationResult
{
    public bool IsValid { get; }
    public IEnumerable<string> Errors { get; }
    
    private ValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? Enumerable.Empty<string>();
    }
    
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors);
}
```

**Step 2: Implement State Validators**
```csharp
// Scripts/State/Validation/HandStateValidator.cs
public class HandStateValidator : IStateValidator<HandState>
{
    public ValidationResult Validate(HandState state)
    {
        var errors = new List<string>();
        
        // Basic validation
        if (state.Cards.Count > state.MaxHandSize)
            errors.Add($"Hand contains {state.Cards.Count} cards, maximum is {state.MaxHandSize}");
            
        // Unique card IDs
        var cardIds = state.Cards.Select(c => c.CardId).ToList();
        if (cardIds.Count != cardIds.Distinct().Count())
            errors.Add("Hand contains duplicate card IDs");
            
        // Dragging validation
        var draggingCount = state.Cards.Count(c => c.IsDragging);
        if (draggingCount > 1)
            errors.Add($"Multiple cards are dragging ({draggingCount}), only one allowed");
            
        // Selection validation (if there are business rules)
        var selectedCount = state.Cards.Count(c => c.IsSelected);
        if (state.IsLocked && selectedCount > 0)
            errors.Add("Hand is locked but has selected cards");
            
        return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }
}
```

**Step 3: Integrate Validation into State Objects**
```csharp
// Scripts/State/HandState.cs (Enhanced)
public class HandState
{
    private static readonly IStateValidator<HandState> Validator = new HandStateValidator();
    
    // ... existing properties ...
    
    public bool IsValid() => Validator.Validate(this).IsValid;
    
    public ValidationResult ValidateDetailed() => Validator.Validate(this);
    
    // Enhanced builder methods with validation
    public HandState WithAddedCard(CardState card)
    {
        if (card == null) throw new ArgumentNullException(nameof(card));
        
        var newCards = Cards.ToList();
        newCards.Add(card);
        var newState = new HandState(newCards, MaxHandSize, IsLocked);
        
        var validationResult = newState.ValidateDetailed();
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Adding card would create invalid state: {string.Join(", ", validationResult.Errors)}");
        }
        
        return newState;
    }
}
```

---

## Phase 2: Extensibility Improvements

### 2.1 Game Phase System Redesign (Week 4-5)

#### **Current Problem: Hard-coded Enum Phases**
```csharp
public enum GamePhase
{
    Menu,
    CardSelection,
    SpellCasting,
    TurnEnd
}
```

#### **Solution: Configurable Phase System**

**Step 1: Create Phase Framework**
```csharp
// Scripts/GamePhases/IGamePhase.cs
public interface IGamePhase
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    bool AllowsCardSelection { get; }
    bool AllowsSpellCasting { get; }
    bool CanPlayerAct { get; }
    TimeSpan? Duration { get; }
    IEnumerable<string> ValidTransitions { get; }
    
    bool CanTransitionTo(IGamePhase targetPhase);
    IEnumerable<ICommand> GetPhaseStartCommands();
    IEnumerable<ICommand> GetPhaseEndCommands();
}

// Scripts/GamePhases/BaseGamePhase.cs
public abstract class BaseGamePhase : IGamePhase
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public virtual bool AllowsCardSelection => false;
    public virtual bool AllowsSpellCasting => false;
    public virtual bool CanPlayerAct => false;
    public virtual TimeSpan? Duration => null;
    public abstract IEnumerable<string> ValidTransitions { get; }
    
    protected BaseGamePhase(string id, string name, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
    
    public virtual bool CanTransitionTo(IGamePhase targetPhase)
    {
        return ValidTransitions.Contains(targetPhase.Id);
    }
    
    public virtual IEnumerable<ICommand> GetPhaseStartCommands() => Enumerable.Empty<ICommand>();
    public virtual IEnumerable<ICommand> GetPhaseEndCommands() => Enumerable.Empty<ICommand>();
}
```

**Step 2: Implement Concrete Phases**
```csharp
// Scripts/GamePhases/CardSelectionPhase.cs
public class CardSelectionPhase : BaseGamePhase
{
    public override bool AllowsCardSelection => true;
    public override bool CanPlayerAct => true;
    public override IEnumerable<string> ValidTransitions => new[] { "SpellCasting", "TurnEnd" };
    
    public CardSelectionPhase() : base("CardSelection", "Card Selection", "Select cards for your spell")
    {
    }
    
    public override IEnumerable<ICommand> GetPhaseStartCommands()
    {
        yield return new RefreshHandLayoutCommand();
        yield return new EnableCardInteractionCommand();
    }
}

// Scripts/GamePhases/SpellCastingPhase.cs
public class SpellCastingPhase : BaseGamePhase
{
    public override bool AllowsSpellCasting => true;
    public override bool CanPlayerAct => false; // Automated phase
    public override TimeSpan? Duration => TimeSpan.FromSeconds(2); // Animation time
    public override IEnumerable<string> ValidTransitions => new[] { "TurnEnd" };
    
    public SpellCastingPhase() : base("SpellCasting", "Spell Casting", "Casting your spell")
    {
    }
    
    public override IEnumerable<ICommand> GetPhaseStartCommands()
    {
        yield return new DisableCardInteractionCommand();
        yield return new ProcessSpellCommand();
    }
}
```

**Step 3: Create Phase Manager**
```csharp
// Scripts/Managers/GamePhaseManager.cs
public class GamePhaseManager : IGamePhaseManager
{
    private readonly Dictionary<string, IGamePhase> _phases = new();
    private readonly ILogger _logger;
    
    public GamePhaseManager(ILogger logger)
    {
        _logger = logger;
        RegisterDefaultPhases();
    }
    
    public void RegisterPhase(IGamePhase phase)
    {
        _phases[phase.Id] = phase;
        _logger.LogInfo($"Registered game phase: {phase.Id} - {phase.Name}");
    }
    
    public IGamePhase GetPhase(string phaseId)
    {
        return _phases.TryGetValue(phaseId, out var phase) ? phase : null;
    }
    
    public bool CanTransition(string fromPhaseId, string toPhaseId)
    {
        var fromPhase = GetPhase(fromPhaseId);
        var toPhase = GetPhase(toPhaseId);
        
        return fromPhase?.CanTransitionTo(toPhase) ?? false;
    }
    
    private void RegisterDefaultPhases()
    {
        RegisterPhase(new MenuPhase());
        RegisterPhase(new CardSelectionPhase());
        RegisterPhase(new SpellCastingPhase());
        RegisterPhase(new TurnEndPhase());
    }
}
```

### 2.2 Status Effect System Overhaul (Week 5-6)

#### **Current Problem: Enum-Based Effects**
```csharp
public enum StatusEffectType
{
    Chill,
    Bleeding,
    Poison
}
```

#### **Solution: Composable Effect System**

**Step 1: Create Effect Framework**
```csharp
// Scripts/StatusEffects/IStatusEffect.cs
public interface IStatusEffect
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    int Stacks { get; }
    float Duration { get; }
    StatusEffectType Type { get; }
    
    IStatusEffect WithStacks(int stacks);
    IStatusEffect WithDuration(float duration);
    bool ShouldExpire(float deltaTime);
    IEnumerable<IAction> GetTickActions();
    IEnumerable<IAction> GetApplyActions();
    IEnumerable<IAction> GetRemoveActions();
}

// Scripts/StatusEffects/BaseStatusEffect.cs
public abstract class BaseStatusEffect : IStatusEffect
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Stacks { get; protected set; }
    public float Duration { get; protected set; }
    public abstract StatusEffectType Type { get; }
    
    protected BaseStatusEffect(string id, string name, string description, int stacks = 1, float duration = 0)
    {
        Id = id;
        Name = name;
        Description = description;
        Stacks = stacks;
        Duration = duration;
    }
    
    public abstract IStatusEffect WithStacks(int stacks);
    public abstract IStatusEffect WithDuration(float duration);
    
    public virtual bool ShouldExpire(float deltaTime)
    {
        Duration -= deltaTime;
        return Duration <= 0 || Stacks <= 0;
    }
    
    public abstract IEnumerable<IAction> GetTickActions();
    public virtual IEnumerable<IAction> GetApplyActions() => Enumerable.Empty<IAction>();
    public virtual IEnumerable<IAction> GetRemoveActions() => Enumerable.Empty<IAction>();
}
```

**Step 2: Implement Concrete Effects**
```csharp
// Scripts/StatusEffects/ChillEffect.cs
public class ChillEffect : BaseStatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Debuff;
    
    public ChillEffect(int stacks = 1, float duration = 3.0f) 
        : base("chill", "Chill", $"Reduces movement speed and increases frost damage taken. Stacks: {stacks}", stacks, duration)
    {
    }
    
    public override IStatusEffect WithStacks(int stacks)
    {
        return new ChillEffect(stacks, Duration);
    }
    
    public override IStatusEffect WithDuration(float duration)
    {
        return new ChillEffect(Stacks, duration);
    }
    
    public override IEnumerable<IAction> GetTickActions()
    {
        // No per-tick actions for chill
        yield break;
    }
    
    public override IEnumerable<IAction> GetApplyActions()
    {
        yield return new ModifyPropertyAction("MovementSpeed", -0.1f * Stacks, ModifyOperation.Multiply);
        yield return new ModifyPropertyAction("FrostResistance", -0.2f * Stacks, ModifyOperation.Add);
    }
    
    public override IEnumerable<IAction> GetRemoveActions()
    {
        yield return new ModifyPropertyAction("MovementSpeed", 0.1f * Stacks, ModifyOperation.Multiply);
        yield return new ModifyPropertyAction("FrostResistance", 0.2f * Stacks, ModifyOperation.Add);
    }
}

// Scripts/StatusEffects/BleedingEffect.cs
public class BleedingEffect : BaseStatusEffect
{
    public override StatusEffectType Type => StatusEffectType.DamageOverTime;
    
    private readonly float _damagePerStack;
    
    public BleedingEffect(int stacks = 1, float duration = 5.0f, float damagePerStack = 2.0f) 
        : base("bleeding", "Bleeding", $"Takes {damagePerStack} damage per stack each turn. Stacks: {stacks}", stacks, duration)
    {
        _damagePerStack = damagePerStack;
    }
    
    public override IStatusEffect WithStacks(int stacks)
    {
        return new BleedingEffect(stacks, Duration, _damagePerStack);
    }
    
    public override IStatusEffect WithDuration(float duration)
    {
        return new BleedingEffect(Stacks, duration, _damagePerStack);
    }
    
    public override IEnumerable<IAction> GetTickActions()
    {
        yield return new DamageAction(_damagePerStack * Stacks, DamageType.Physical);
    }
}
```

**Step 3: Create Effect Registry**
```csharp
// Scripts/StatusEffects/StatusEffectRegistry.cs
public class StatusEffectRegistry : IStatusEffectRegistry
{
    private readonly Dictionary<string, Func<IStatusEffect>> _effectFactories = new();
    private readonly ILogger _logger;
    
    public StatusEffectRegistry(ILogger logger)
    {
        _logger = logger;
        RegisterDefaultEffects();
    }
    
    public void RegisterEffect<T>(string id, Func<T> factory) where T : IStatusEffect
    {
        _effectFactories[id] = factory;
        _logger.LogInfo($"Registered status effect: {id}");
    }
    
    public IStatusEffect CreateEffect(string id, int stacks = 1, float duration = 0)
    {
        if (!_effectFactories.TryGetValue(id, out var factory))
        {
            throw new ArgumentException($"Unknown status effect: {id}");
        }
        
        var effect = factory();
        return effect.WithStacks(stacks).WithDuration(duration);
    }
    
    private void RegisterDefaultEffects()
    {
        RegisterEffect("chill", () => new ChillEffect());
        RegisterEffect("bleeding", () => new BleedingEffect());
        RegisterEffect("poison", () => new PoisonEffect());
    }
}
```

---

## Phase 3: Performance & Polish

### 3.1 Performance Optimization (Week 7-8)

#### **Address LINQ and Object Creation Issues**

**Step 1: Optimize State Updates**
```csharp
// Scripts/State/HandState.cs (Optimized)
public class HandState
{
    // Cache frequently accessed collections
    private IReadOnlyList<CardState> _selectedCards;
    private bool _selectedCardsCacheValid;
    
    public IEnumerable<CardState> SelectedCards 
    {
        get
        {
            if (!_selectedCardsCacheValid)
            {
                _selectedCards = Cards.Where(card => card.IsSelected).ToList().AsReadOnly();
                _selectedCardsCacheValid = true;
            }
            return _selectedCards;
        }
    }
    
    // Optimized builder methods
    public HandState WithCardSelection(string cardId, bool isSelected)
    {
        // Only create new state if actually changing
        var targetCard = Cards.FirstOrDefault(c => c.CardId == cardId);
        if (targetCard == null || targetCard.IsSelected == isSelected)
            return this; // No change needed
            
        // Use array for better performance than LINQ chains
        var newCards = new CardState[Cards.Count];
        for (int i = 0; i < Cards.Count; i++)
        {
            var card = Cards[i];
            newCards[i] = card.CardId == cardId ? card.WithSelection(isSelected) : card;
        }
        
        return new HandState(newCards, MaxHandSize, IsLocked);
    }
}
```

**Step 2: Implement Object Pooling**
```csharp
// Scripts/Utils/ObjectPool.cs
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _objectGenerator;
    private readonly Action<T> _resetAction;
    
    public ObjectPool(Func<T> objectGenerator = null, Action<T> resetAction = null)
    {
        _objectGenerator = objectGenerator ?? (() => new T());
        _resetAction = resetAction ?? (_ => { });
    }
    
    public T Rent()
    {
        return _objects.TryDequeue(out var item) ? item : _objectGenerator();
    }
    
    public void Return(T item)
    {
        _resetAction(item);
        _objects.Enqueue(item);
    }
}

// Usage in frequently created objects
public class EventPool
{
    private static readonly ObjectPool<List<IGameEvent>> _eventListPool = 
        new(() => new List<IGameEvent>(), list => list.Clear());
        
    public static List<IGameEvent> RentEventList() => _eventListPool.Rent();
    public static void ReturnEventList(List<IGameEvent> list) => _eventListPool.Return(list);
}
```

### 3.2 Testing Infrastructure (Week 8-9)

#### **Create Comprehensive Test Framework**

**Step 1: Test Base Classes**
```csharp
// Tests/TestBase.cs
public abstract class TestBase
{
    protected IDependencyContainer Container { get; private set; }
    protected Mock<ILogger> MockLogger { get; private set; }
    protected Mock<IEventBus> MockEventBus { get; private set; }
    
    [SetUp]
    public virtual void Setup()
    {
        Container = new DependencyContainer();
        MockLogger = new Mock<ILogger>();
        MockEventBus = new Mock<IEventBus>();
        
        Container.RegisterInstance<ILogger>(MockLogger.Object);
        Container.RegisterInstance<IEventBus>(MockEventBus.Object);
        
        RegisterTestServices();
        Container.BuildContainer();
    }
    
    protected abstract void RegisterTestServices();
    
    protected IGameStateData CreateTestGameState(
        HandState handState = null,
        PlayerState playerState = null,
        GamePhaseState phaseState = null)
    {
        return GameState.Create(
            handState ?? new HandState(),
            playerState ?? new PlayerState(),
            phaseState ?? new GamePhaseState()
        );
    }
}
```

**Step 2: Command Tests**
```csharp
// Tests/Commands/SelectCardCommandTests.cs
[TestFixture]
public class SelectCardCommandTests : TestBase
{
    private SelectCardCommand _command;
    private IGameStateData _testState;
    
    protected override void RegisterTestServices()
    {
        // Register test-specific services
    }
    
    [SetUp]
    public override void Setup()
    {
        base.Setup();
        
        var testCard = new CardState("test-card", Mock.Of<SpellCardResource>(), false, false, 0);
        var handState = new HandState(new[] { testCard });
        _testState = CreateTestGameState(handState: handState);
        
        _command = new SelectCardCommand("test-card", MockLogger.Object);
    }
    
    [Test]
    public void CanExecute_WithValidCard_ReturnsTrue()
    {
        // Arrange
        var gameState = _testState;
        
        // Act
        var result = _command.CanExecute(gameState);
        
        // Assert
        Assert.That(result, Is.True);
    }
    
    [Test]
    public void Execute_SelectsCard_ReturnsStateWithSelectedCard()
    {
        // Arrange
        var gameState = _testState;
        
        // Act
        var result = _command.Execute(gameState);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.NewState.Hand.SelectedCount, Is.EqualTo(1));
        Assert.That(result.NewState.Hand.Cards.First().IsSelected, Is.True);
    }
}
```

---

## Implementation Timeline Summary

### Week 1-3: Foundation Fixes
- ✅ Dependency injection implementation
- ✅ Command pattern restoration  
- ✅ Interface standardization
- ✅ State validation enhancement

### Week 4-6: Extensibility Improvements
- ✅ Game phase system redesign
- ✅ Status effect system overhaul
- ✅ Action system enhancement
- ✅ Event system standardization

### Week 7-9: Performance & Polish
- ✅ Performance optimization
- ✅ Testing infrastructure
- ✅ Development tools
- ✅ Documentation updates

## Risk Mitigation

### High Risk Areas
1. **Breaking Changes**: Major refactoring will break existing integrations
   - **Mitigation**: Implement changes incrementally with backward compatibility layers
   
2. **Performance Regressions**: New abstractions might impact performance
   - **Mitigation**: Benchmark before/after, implement performance tests
   
3. **Complexity Increase**: Additional abstractions increase learning curve
   - **Mitigation**: Comprehensive documentation and examples

### Success Criteria
- ✅ All existing functionality preserved
- ✅ New features can be added without modifying core systems
- ✅ Unit test coverage > 80%
- ✅ No performance regressions in critical paths
- ✅ Developer can add new status effect in < 30 minutes
- ✅ Developer can add new game phase in < 1 hour

This implementation plan provides a systematic approach to addressing all critical issues while maintaining system stability and improving long-term maintainability.