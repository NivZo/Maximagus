# Status Effects & Hand Management Implementation Guide

## Overview
This guide provides comprehensive instructions for implementing status effects and hand management features in the spell card game. The implementation should be extensible, maintainable, and follow the existing architectural patterns (ServiceLocator, EventBus, Resource-based actions).

## ⚠️ Important Implementation Notes

**This guide serves as a foundation and reference - NOT as a complete copy-paste solution.** You will need to:

- **Analyze the existing codebase** to understand current patterns, naming conventions, and architectural decisions
- **Adapt the provided code examples** to match the actual project structure and existing interfaces
- **Fill in missing pieces** that aren't explicitly covered in this guide (e.g., error handling, edge cases, UI integration points)
- **Think critically** about how these systems integrate with existing code - you may need to modify existing classes beyond what's shown here
- **Resolve dependencies** and ensure all necessary imports, interfaces, and base classes exist or are created
- **Test thoroughly** and debug integration issues that may arise from combining new and existing systems

**The code examples are illustrative** - they show the structure and approach, but you must:
- Verify method signatures match existing patterns
- Ensure proper inheritance hierarchies
- Handle missing enums, interfaces, or base classes
- Adapt to any existing validation or error handling patterns
- Consider performance implications and optimize as needed

**When in doubt**, prioritize consistency with existing code patterns over the exact implementation shown in this guide.

## 1. Status Effects System

### 1.1 Core Status Effect Architecture

Create the following new files and enums:

#### `StatusEffectType.cs` (Enum)
```csharp
namespace Maximagus.Scripts.Enums
{
    public enum StatusEffectType
    {
        Poison,        // Deal damage over time
        Bleeding,      // Deal damage on any damage instance
        Regeneration,  // Heal over time
        Shield,        // Absorb damage
        Vulnerability, // Take extra damage
        // Extensible for future effects
    }
}
```

#### `StatusEffectTrigger.cs` (Enum)
```csharp
namespace Maximagus.Scripts.Enums
{
    public enum StatusEffectTrigger
    {
        StartOfTurn,
        EndOfTurn,
        OnDamageDealt,
        OnDamageReceived,
        OnSpellCast,
        // Extensible for future triggers
    }
}
```

#### `StatusEffectDecayMode.cs` (Enum)
```csharp
namespace Maximagus.Scripts.Enums
{
    public enum StatusEffectDecayMode
    {
        Never,              // Permanent until manually removed
        EndOfTurn,          // Removed completely at end of turn
        ReduceByOneOnTrigger, // Reduce stacks by 1 when triggered
        ReduceByOneEndOfTurn  // Reduce stacks by 1 at end of turn
    }
}
```

### 1.2 Status Effect Resource Definition

#### `StatusEffectResource.cs`

**Note: This is a starting template - you'll need to adapt it based on your existing Resource patterns and may need to create additional derived classes for specific effect types.**

```csharp
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.StatusEffects
{
    [GlobalClass]
    public partial class StatusEffectResource : Resource
    {
        [Export] public StatusEffectType EffectType { get; set; }
        [Export] public string EffectName { get; set; }
        [Export] public string Description { get; set; }
        [Export] public StatusEffectTrigger Trigger { get; set; }
        [Export] public StatusEffectDecayMode DecayMode { get; set; }
        [Export] public bool IsStackable { get; set; } = true;
        [Export] public int InitialStacks { get; set; } = 1;
        [Export] public float Value { get; set; }
        [Export] public int MaxStacks { get; set; } = 99;

        public virtual void OnTrigger(SpellContext context, int stacks)
        {
            // Base implementation - you'll likely need to create derived classes
            // for complex effects or integrate with existing damage/healing systems
            switch (EffectType)
            {
                case StatusEffectType.Poison:
                    var poisonDamage = Value * stacks;
                    GD.Print($"Poison deals {poisonDamage} damage ({stacks} stacks)");
                    context.TotalDamageDealt += poisonDamage;
                    break;
                    
                case StatusEffectType.Bleeding:
                    // This will need integration with your actual damage system
                    var bleedDamage = Value * stacks;
                    GD.Print($"Bleeding adds {bleedDamage} damage to attack ({stacks} stacks)");
                    break;
                    
                // TODO: Implement other effect types based on your game's needs
            }
        }
    }
}
```

**Implementation considerations:**
- You may need to create specific derived classes for complex effects
- Integration with existing damage/healing systems may require additional methods
- Consider how this interacts with your existing modifier system

### 1.3 Status Effect Instance Management

#### `StatusEffectInstance.cs`
```csharp
using Godot;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Maximagus.Scripts.StatusEffects
{
    public partial class StatusEffectInstance : RefCounted
    {
        public StatusEffectResource Effect { get; set; }
        public int CurrentStacks { get; set; }
        public string InstanceId { get; set; }

        public StatusEffectInstance(StatusEffectResource effect, int stacks = 1)
        {
            Effect = effect;
            CurrentStacks = stacks;
            InstanceId = System.Guid.NewGuid().ToString();
        }

        public void AddStacks(int amount)
        {
            if (Effect.IsStackable)
            {
                CurrentStacks = Mathf.Min(CurrentStacks + amount, Effect.MaxStacks);
            }
        }

        public void ReduceStacks(int amount = 1)
        {
            CurrentStacks = Mathf.Max(0, CurrentStacks - amount);
        }

        public bool IsExpired => CurrentStacks <= 0;
    }
}
```

### 1.4 Status Effect Manager

#### `StatusEffectManager.cs`
```csharp
using Godot;
using Godot.Collections;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.StatusEffects;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Managers
{
    public partial class StatusEffectManager : Node
    {
        private Array<StatusEffectInstance> _activeEffects = new();

        public void AddStatusEffect(StatusEffectResource effect, int stacks = 1)
        {
            var existingEffect = _activeEffects.FirstOrDefault(e => 
                e.Effect.EffectType == effect.EffectType && e.Effect.IsStackable);

            if (existingEffect != null && effect.IsStackable)
            {
                existingEffect.AddStacks(stacks);
                GD.Print($"Added {stacks} stacks to {effect.EffectName}. Total: {existingEffect.CurrentStacks}");
            }
            else
            {
                var newInstance = new StatusEffectInstance(effect, stacks);
                _activeEffects.Add(newInstance);
                GD.Print($"Applied new status effect: {effect.EffectName} with {stacks} stacks");
            }
        }

        public void TriggerEffects(StatusEffectTrigger trigger, SpellContext context)
        {
            var effectsToRemove = new Array<StatusEffectInstance>();

            foreach (var effectInstance in _activeEffects)
            {
                if (effectInstance.Effect.Trigger == trigger)
                {
                    effectInstance.Effect.OnTrigger(context, effectInstance.CurrentStacks);

                    // Handle decay
                    if (effectInstance.Effect.DecayMode == StatusEffectDecayMode.ReduceByOneOnTrigger)
                    {
                        effectInstance.ReduceStacks();
                    }

                    if (effectInstance.IsExpired)
                    {
                        effectsToRemove.Add(effectInstance);
                    }
                }
            }

            // Remove expired effects
            foreach (var expiredEffect in effectsToRemove)
            {
                _activeEffects.Remove(expiredEffect);
                GD.Print($"Removed expired status effect: {expiredEffect.Effect.EffectName}");
            }
        }

        public void ProcessEndOfTurnDecay()
        {
            var effectsToRemove = new Array<StatusEffectInstance>();

            foreach (var effectInstance in _activeEffects)
            {
                switch (effectInstance.Effect.DecayMode)
                {
                    case StatusEffectDecayMode.EndOfTurn:
                        effectsToRemove.Add(effectInstance);
                        break;
                    case StatusEffectDecayMode.ReduceByOneEndOfTurn:
                        effectInstance.ReduceStacks();
                        if (effectInstance.IsExpired)
                            effectsToRemove.Add(effectInstance);
                        break;
                }
            }

            foreach (var expiredEffect in effectsToRemove)
            {
                _activeEffects.Remove(expiredEffect);
                GD.Print($"End of turn removed: {expiredEffect.Effect.EffectName}");
            }
        }

        public Array<StatusEffectInstance> GetActiveEffects() => _activeEffects;
        
        public int GetStacksOfEffect(StatusEffectType effectType)
        {
            return _activeEffects
                .Where(e => e.Effect.EffectType == effectType)
                .Sum(e => e.CurrentStacks);
        }
    }
}
```

### 1.5 Status Effect Action

#### `StatusEffectActionResource.cs`
```csharp
using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class StatusEffectActionResource : ActionResource
    {
        [Export] public StatusEffectResource StatusEffect { get; set; }
        [Export] public int Stacks { get; set; } = 1;

        public override void Execute(SpellContext context)
        {
            GD.Print($"Applying status effect: {StatusEffect.EffectName}");
            var statusManager = ServiceLocator.GetService<StatusEffectManager>();
            statusManager?.AddStatusEffect(StatusEffect, Stacks);
        }
    }
}
```

## 2. Turn Management System

### 2.1 Turn Phase System

#### `TurnPhase.cs` (Enum)
```csharp
namespace Maximagus.Scripts.Enums
{
    public enum TurnPhase
    {
        TurnStart,
        SpellExecution,
        TurnEnd,
        // Extensible for future phases
    }
}
```

### 2.2 Turn Events

#### `TurnEvents.cs`
```csharp
namespace Maximagus.Scripts.Events
{
    public class TurnStartedEvent
    {
        public int TurnNumber { get; set; }
    }

    public class TurnEndedEvent
    {
        public int TurnNumber { get; set; }
    }

    public class TurnPhaseChangedEvent
    {
        public TurnPhase PreviousPhase { get; set; }
        public TurnPhase NewPhase { get; set; }
        public int TurnNumber { get; set; }
    }
}
```

### 2.3 Turn Manager

#### `TurnManager.cs`

**Implementation Note: You'll need to determine how this integrates with your game's main loop and existing scene structure. This may need to be a singleton or attached to a specific scene node.**

```csharp
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;
using Maximagus.Scripts.Managers;

namespace Maximagus.Scripts.Managers
{
    public partial class TurnManager : Node
    {
        private int _currentTurn = 1;
        private TurnPhase _currentPhase = TurnPhase.TurnStart;
        private StatusEffectManager _statusEffectManager;
        private IEventBus _eventBus;

        public override void _Ready()
        {
            _statusEffectManager = ServiceLocator.GetService<StatusEffectManager>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            
            // TODO: You may need to subscribe to game events or integrate with existing game flow
        }

        public void StartTurn()
        {
            _currentPhase = TurnPhase.TurnStart;
            _eventBus.Publish(new TurnStartedEvent { TurnNumber = _currentTurn });
            _eventBus.Publish(new TurnPhaseChangedEvent 
            { 
                PreviousPhase = TurnPhase.TurnEnd, 
                NewPhase = TurnPhase.TurnStart, 
                TurnNumber = _currentTurn 
            });

            // Trigger start of turn status effects
            var context = new Scripts.Spells.Implementations.SpellContext();
            // TODO: You may need to pass additional context data here
            _statusEffectManager?.TriggerEffects(StatusEffectTrigger.StartOfTurn, context);

            GD.Print($"Turn {_currentTurn} started");
        }

        public void EnterSpellExecutionPhase()
        {
            var previousPhase = _currentPhase;
            _currentPhase = TurnPhase.SpellExecution;
            _eventBus.Publish(new TurnPhaseChangedEvent 
            { 
                PreviousPhase = previousPhase, 
                NewPhase = TurnPhase.SpellExecution, 
                TurnNumber = _currentTurn 
            });
        }

        public void EndTurn()
        {
            _currentPhase = TurnPhase.TurnEnd;
            _eventBus.Publish(new TurnPhaseChangedEvent 
            { 
                PreviousPhase = TurnPhase.SpellExecution, 
                NewPhase = TurnPhase.TurnEnd, 
                TurnNumber = _currentTurn 
            });

            // Trigger end of turn status effects
            var context = new Scripts.Spells.Implementations.SpellContext();
            // TODO: Consider if you need to aggregate context data from the turn
            _statusEffectManager?.TriggerEffects(StatusEffectTrigger.EndOfTurn, context);
            
            // Process status effect decay
            _statusEffectManager?.ProcessEndOfTurnDecay();

            _eventBus.Publish(new TurnEndedEvent { TurnNumber = _currentTurn });
            
            _currentTurn++;
            GD.Print($"Turn ended. Next turn: {_currentTurn}");
            
            // TODO: Determine if you want automatic turn progression or manual control
        }

        public int CurrentTurn => _currentTurn;
        public TurnPhase CurrentPhase => _currentPhase;
    }
}
```

**Critical implementation considerations:**
- Determine where and how turn progression is triggered in your game flow
- Consider if turns should advance automatically or require manual triggering
- You may need to integrate with existing game state management
- Think about save/load implications for turn state

## 3. Hand Management System

### 3.1 Hand Management Enums and Events

#### `HandActionType.cs` (Enum)
```csharp
namespace Maximagus.Scripts.Enums
{
    public enum HandActionType
    {
        Play,
        Discard
    }
}
```

#### `HandEvents.cs`
```csharp
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Events
{
    public class HandSubmittedEvent
    {
        public Array<SpellCardResource> Cards { get; set; }
        public HandActionType ActionType { get; set; }
    }

    public class HandLimitReachedEvent
    {
        public int RemainingHands { get; set; }
        public int RemainingDiscards { get; set; }
    }

    public class CardsRedrawEvent
    {
        public Array<SpellCardResource> NewCards { get; set; }
    }
}
```

### 3.2 Hand Manager

#### `HandManager.cs`
```csharp
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;

namespace Maximagus.Scripts.Managers
{
    public partial class HandManager : Node
    {
        [Export] public int MaxHandsPerEncounter { get; set; } = 5;
        [Export] public int MaxDiscardsPerEncounter { get; set; } = 5;
        [Export] public int MaxCardsInHand { get; set; } = 10;
        [Export] public int MaxCardsPerSubmission { get; set; } = 4;

        private int _remainingHands;
        private int _remainingDiscards;
        private Array<SpellCardResource> _currentHand = new();
        private IEventBus _eventBus;

        public override void _Ready()
        {
            _eventBus = ServiceLocator.GetService<IEventBus>();
            ResetForNewEncounter();
        }

        public void ResetForNewEncounter()
        {
            _remainingHands = MaxHandsPerEncounter;
            _remainingDiscards = MaxDiscardsPerEncounter;
            RedrawHand();
            GD.Print($"Hand Manager reset: {_remainingHands} hands, {_remainingDiscards} discards available");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            return actionType switch
            {
                HandActionType.Play => _remainingHands > 0,
                HandActionType.Discard => _remainingDiscards > 0,
                _ => false
            };
        }

        public bool SubmitHand(Array<SpellCardResource> selectedCards, HandActionType actionType)
        {
            if (!CanSubmitHand(actionType))
            {
                GD.Print($"Cannot submit hand: no {actionType} actions remaining");
                return false;
            }

            if (selectedCards.Count == 0 || selectedCards.Count > MaxCardsPerSubmission)
            {
                GD.Print($"Invalid card count: {selectedCards.Count} (max: {MaxCardsPerSubmission})");
                return false;
            }

            // Validate all selected cards are in current hand
            foreach (var card in selectedCards)
            {
                if (!_currentHand.Contains(card))
                {
                    GD.Print($"Card {card.CardName} not in current hand");
                    return false;
                }
            }

            // Deduct action count
            if (actionType == HandActionType.Play)
                _remainingHands--;
            else
                _remainingDiscards--;

            // Remove selected cards from hand
            foreach (var card in selectedCards)
            {
                _currentHand.Remove(card);
            }

            // Publish event
            _eventBus.Publish(new HandSubmittedEvent 
            { 
                Cards = selectedCards, 
                ActionType = actionType 
            });

            GD.Print($"{actionType} action completed. Remaining: {_remainingHands} hands, {_remainingDiscards} discards");

            // Redraw to fill hand
            RedrawHand();

            // Check if limits reached
            if (_remainingHands == 0 && _remainingDiscards == 0)
            {
                _eventBus.Publish(new HandLimitReachedEvent 
                { 
                    RemainingHands = _remainingHands, 
                    RemainingDiscards = _remainingDiscards 
                });
            }

            return true;
        }

        private void RedrawHand()
        {
            // Fill hand up to MaxCardsInHand
            var cardsNeeded = MaxCardsInHand - _currentHand.Count;
            var newCards = GenerateRandomCards(cardsNeeded);
            
            foreach (var card in newCards)
            {
                _currentHand.Add(card);
            }

            _eventBus.Publish(new CardsRedrawEvent { NewCards = newCards });
            GD.Print($"Redrawn {cardsNeeded} cards. Hand size: {_currentHand.Count}");
        }

        private Array<SpellCardResource> GenerateRandomCards(int count)
        {
            // Placeholder implementation - replace with actual card generation logic
            var cards = new Array<SpellCardResource>();
            for (int i = 0; i < count; i++)
            {
                // This should use your actual card generation system
                var card = new SpellCardResource();
                card.CardName = $"Random Card {i}";
                cards.Add(card);
            }
            return cards;
        }

        public Array<SpellCardResource> GetCurrentHand() => _currentHand;
        public int RemainingHands => _remainingHands;
        public int RemainingDiscards => _remainingDiscards;
    }
}
```

## 4. Integration with Existing Systems

### 4.1 Update SpellContext

**You'll need to modify the existing `SpellContext.cs` file.** Add status effect integration:

```csharp
// Add this property to your existing SpellContext class
public StatusEffectManager StatusEffectManager { get; set; }

// Add this method to your existing SpellContext class
public void TriggerStatusEffects(StatusEffectTrigger trigger)
{
    StatusEffectManager?.TriggerEffects(trigger, this);
}
```

**Important:** Review your existing SpellContext to ensure these additions don't conflict with existing patterns or properties.

### 4.2 Update SpellProcessor

**Modify your existing `SpellProcessor.cs`** - don't replace it entirely. The key changes needed:

```csharp
public void ProcessSpell(Array<SpellCardResource> cards)
{
    GD.Print("--- Processing Spell ---");
    var context = new SpellContext();
    
    // NEW: Add status effect manager to context
    context.StatusEffectManager = ServiceLocator.GetService<StatusEffectManager>();
    
    // NEW: Integrate with turn management
    var turnManager = ServiceLocator.GetService<TurnManager>();
    turnManager?.EnterSpellExecutionPhase();

    // NEW: Trigger pre-spell status effects (if this makes sense for your game)
    context.TriggerStatusEffects(StatusEffectTrigger.OnSpellCast);

    // EXISTING: Keep your existing spell processing logic
    GD.Print($"Executing {cards.Count()} cards in the following order: {string.Join(", ", cards.Select(c => c.CardName))}");

    foreach (var card in cards)
    {
        GD.Print($"- Executing card: {card.CardName}");
        card.Execute(context);
        
        // NEW: Consider if you want to trigger status effects after each card or after all cards
        context.TriggerStatusEffects(StatusEffectTrigger.OnDamageDealt);
    }

    GD.Print($"Spell total damage dealt: {context.TotalDamageDealt}");
    GD.Print("--- Spell Finished ---");
}
```

**Critical considerations:**
- Determine the right timing for status effect triggers in your spell execution flow
- Consider if you need different triggers for different points in spell execution
- Think about how this affects your existing modifier system

### 4.3 Update DamageActionResource

**You'll need to modify your existing `DamageActionResource.cs`** to integrate status effects. This is a complex integration - consider carefully:

```csharp
public override void Execute(SpellContext context)
{
    // NEW: Trigger status effects that activate when damage is about to be dealt
    context.TriggerStatusEffects(StatusEffectTrigger.OnDamageReceived);
    
    // EXISTING: Keep your existing damage calculation
    var finalDamage = context.ApplyDamageModifiers(this);
    
    // NEW: Consider how status effects modify damage
    // This is just an example - you'll need to design how bleeding/poison affects damage
    var bleedingStacks = context.StatusEffectManager?.GetStacksOfEffect(StatusEffectType.Bleeding) ?? 0;
    if (bleedingStacks > 0)
    {
        finalDamage += bleedingStacks; // Or whatever bleeding formula you want
        GD.Print($"Bleeding added {bleedingStacks} damage");
    }
    
    // EXISTING: Keep your existing damage application logic
    GD.Print($"Dealt {finalDamage} damage of type {DamageType}.");
    context.TotalDamageDealt += finalDamage;

    // EXISTING: Keep your existing context property logic
    if (finalDamage > 0)
    {
        var damageDealtContextProperty = DamageType switch
        {
            DamageType.Fire => ContextProperty.FireDamageDealt,
            DamageType.Frost => ContextProperty.FrostDamageDealt,
            _ => throw new System.Exception($"No context property implemented for damage type {DamageType}")
        };

        context.ModifyProperty(damageDealtContextProperty, finalDamage, ContextPropertyOperation.Add);
    }
}
```

**This is the most complex integration point** - you'll need to carefully consider:
- How status effects interact with your existing modifier system
- Whether status effects should be applied before or after modifiers
- How different damage types interact with different status effects
- Performance implications of status effect checks on every damage instance

## 5. Service Registration

**You'll need to update your existing `ServiceLocator.cs`** to register the new managers. The timing and initialization order may be important:

```csharp
public static void Initialize()
{
    // EXISTING: Keep your existing service registrations
    RegisterService<ILogger>(new GodotLogger());
    RegisterService<IEventBus>(new SimpleEventBus());
    RegisterService<IHoverManager>(new HoverManager(GetService<ILogger>()));
    RegisterService<IDragManager>(new DragManager(GetService<ILogger>()));
    
    // NEW: Add new services - consider initialization order dependencies
    RegisterService<StatusEffectManager>(new StatusEffectManager());
    RegisterService<TurnManager>(new TurnManager());
    RegisterService<HandManager>(new HandManager());

    GD.Print($"Initialized {_services.Count} Services");
}
```

**Important considerations:**
- You may need to handle manager dependencies (e.g., StatusEffectManager might need to be initialized before TurnManager)
- Consider if these managers need to be Node-based or if they can be plain C# classes
- Think about cleanup/disposal when the game ends

## 6. Input Handling Integration

Create an input handler that can distinguish between play and discard actions:

#### `GameInputHandler.cs`
```csharp
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Maximagus.Scripts.Input
{
    public partial class GameInputHandler : Node
    {
        private HandManager _handManager;
        private SpellProcessor _spellProcessor;
        private Array<SpellCardResource> _selectedCards = new();

        public override void _Ready()
        {
            _handManager = ServiceLocator.GetService<HandManager>();
            _spellProcessor = GetNode<SpellProcessor>("SpellProcessor"); // Adjust path as needed
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Enter:
                        HandlePlayAction();
                        break;
                    case Key.Delete:
                        HandleDiscardAction();
                        break;
                }
            }
        }

        private void HandlePlayAction()
        {
            if (_handManager.SubmitHand(_selectedCards, HandActionType.Play))
            {
                _spellProcessor.ProcessSpell(_selectedCards);
                _selectedCards.Clear();
            }
        }

        private void HandleDiscardAction()
        {
            if (_handManager.SubmitHand(_selectedCards, HandActionType.Discard))
            {
                GD.Print("Cards discarded successfully");
                _selectedCards.Clear();
            }
        }

        public void SelectCard(SpellCardResource card)
        {
            if (!_selectedCards.Contains(card))
                _selectedCards.Add(card);
        }

        public void DeselectCard(SpellCardResource card)
        {
            _selectedCards.Remove(card);
        }
    }
}
```

## 7. Testing and Validation

### 7.1 Create Test Status Effects

Create some example status effect resources in Godot:

1. **Poison Effect**: 
   - Type: Poison
   - Trigger: EndOfTurn
   - DecayMode: ReduceByOneEndOfTurn
   - Value: 3 (damage per stack)
   - Stackable: true

2. **Bleeding Effect**:
   - Type: Bleeding
   - Trigger: OnDamageDealt
   - DecayMode: Never
   - Value: 1 (bonus damage per stack)
   - Stackable: true

3. **Shield Effect**:
   - Type: Shield
   - Trigger: OnDamageReceived
   - DecayMode: ReduceByOneOnTrigger
   - Value: 5 (damage absorbed per stack)
   - Stackable: true

### 7.2 Testing Checklist

- [ ] Status effects can be applied and stack correctly
- [ ] Status effects trigger at appropriate times
- [ ] Status effects decay according to their settings
- [ ] Hand submission works for both play and discard
- [ ] Hand limits are enforced correctly
- [ ] Cards are redrawn after submission
- [ ] Turn phases transition correctly
- [ ] Events are published appropriately

## 8. Extension Points

The system is designed to be easily extensible:

### 8.1 Adding New Status Effects
1. Add new `StatusEffectType` enum value
2. Create derived `StatusEffectResource` with custom `OnTrigger` logic
3. No changes needed to existing systems

### 8.2 Adding New Turn Phases
1. Add new `TurnPhase` enum value
2. Update `TurnManager` to handle new phase
3. Add corresponding triggers if needed

### 8.3 Adding New Triggers
1. Add new `StatusEffectTrigger` enum value
2. Call `TriggerEffects` at appropriate places in game logic
3. No changes needed to existing status effects

## 9. Architecture Notes & Final Implementation Guidance

### Design Principles
- **Separation of Concerns**: Each manager handles its specific domain
- **Event-Driven**: Uses EventBus for loose coupling between systems
- **Resource-Based**: Follows existing pattern of using Godot Resources for data
- **Service Locator**: Maintains singleton pattern for manager access
- **Extensibility**: Enum-based systems allow easy addition of new types without breaking changes
- **Debuggability**: Extensive logging for development and troubleshooting

### Critical Implementation Reminders

**You are responsible for:**

1. **Ensuring all dependencies exist** - Create missing interfaces, enums, and base classes as needed
2. **Proper error handling** - Add try-catch blocks, null checks, and validation where appropriate
3. **Performance optimization** - Consider the performance impact of status effect checks on every action
4. **Thread safety** - If your game uses multithreading, ensure proper synchronization
5. **Memory management** - Ensure proper cleanup of status effects and event subscriptions
6. **Testing integration points** - The intersections between systems are the most likely places for bugs

**Pay special attention to:**
- How the new systems integrate with your existing game loop
- Whether Node-based managers vs plain C# classes work better in your architecture
- How events are subscribed/unsubscribed to prevent memory leaks
- The order of operations when multiple systems interact (modifiers vs status effects vs damage)

**If something doesn't work:**
- Check existing code patterns in your project first
- Consider simpler approaches if the suggested solution is too complex
- Don't hesitate to modify the architecture if it doesn't fit your specific needs
- Remember that this is a guide, not gospel - adapt as needed

**Missing pieces you'll definitely need to handle:**
- Integration with your UI system for showing status effects
- Save/load functionality for game state including status effects and turn state
- Balancing and tuning of status effect values
- Animation and visual feedback systems
- Potential networking considerations if this becomes multiplayer

This implementation provides a solid foundation that can be extended and modified as gameplay requirements evolve. **The key is to start simple, test thoroughly, and iterate based on what actually works in your specific project context.**