# Design Document

## Overview

This design outlines the migration of spell processing and status effect management from local state management to centralized state management through the GameCommandProcessor. The migration transforms the current SpellContext-based system into a command-driven architecture where all spell and status effect state is managed within the main GameState, while maintaining identical frontend behavior.

The key architectural change involves replacing direct method calls and local state mutations with command-based state updates, introducing dedicated manager classes for business logic separation, and enabling card visuals to respond to state changes rather than direct method invocations.

## Architecture

### State Structure Extensions

The existing GameState will be extended with two new state sections:

```csharp
public interface IGameStateData
{
    // Existing properties...
    SpellState Spell { get; }
    StatusEffectsState StatusEffects { get; }
    
    // New state update methods
    IGameStateData WithSpell(SpellState newSpellState);
    IGameStateData WithStatusEffects(StatusEffectsState newStatusEffectsState);
}
```

### SpellState Structure

```csharp
public class SpellState
{
    public bool IsActive { get; }
    public Dictionary<string, Variant> Properties { get; }
    public ImmutableArray<ModifierData> ActiveModifiers { get; }
    public float TotalDamageDealt { get; }
    public ImmutableArray<SpellHistoryEntry> History { get; }
    public DateTime? StartTime { get; }
    public int CurrentActionIndex { get; }
}

public class ModifierData
{
    public ModifierType Type { get; }
    public DamageType Element { get; }
    public float Value { get; }
    public bool IsConsumedOnUse { get; }
    public ImmutableArray<SpellModifierCondition> Conditions { get; }
}

public class SpellHistoryEntry
{
    public DateTime CompletedAt { get; }
    public float TotalDamage { get; }
    public Dictionary<string, Variant> FinalProperties { get; }
    public ImmutableArray<string> CastCardIds { get; }
    public ImmutableArray<SpellCardResource> CastCardResources { get; }
    public bool WasSuccessful { get; }
    public string ErrorMessage { get; }
}
```

### StatusEffectsState Structure

```csharp
public class StatusEffectsState
{
    public ImmutableArray<StatusEffectInstanceData> ActiveEffects { get; }
}

public class StatusEffectInstanceData
{
    public StatusEffectType EffectType { get; }
    public int CurrentStacks { get; }
    public StatusEffectResource EffectResource { get; }
    public DateTime AppliedAt { get; }
}
```

## Components and Interfaces

### Command System Extensions

New commands will be created to handle spell and status effect operations:

#### Spell Commands
- `StartSpellCommand` - Initializes spell state when spell casting begins
- `ExecuteCardActionCommand` - Processes individual card actions and updates spell state
- `CompleteSpellCommand` - Finalizes spell, moves to history, and clears active state
- `UpdateSpellPropertyCommand` - Updates spell context properties
- `AddSpellModifierCommand` - Adds modifiers to active spell state

#### Status Effect Commands
- `ApplyStatusEffectCommand` - Adds or updates status effects in state
- `TriggerStatusEffectsCommand` - Processes status effect triggers and updates state
- `ProcessStatusEffectDecayCommand` - Handles end-of-turn decay and expiration

### Manager Classes for Business Logic

#### SpellLogicManager (Static)
Replaces SpellContext logic with pure functions that operate on complete game state:

```csharp
public static class SpellLogicManager
{
    public static float CalculateModifiedDamage(
        DamageActionResource damageAction, 
        IGameStateData gameState);
    
    public static (float finalDamage, ImmutableArray<ModifierData> remainingModifiers) 
        ApplyDamageModifiers(
            DamageActionResource damageAction, 
            IGameStateData gameState);
    
    public static SpellState AddModifier(SpellState currentState, ModifierData modifier);
    
    public static SpellState UpdateProperty(
        SpellState currentState, 
        string key, 
        Variant value, 
        ContextPropertyOperation operation);
        
    public static SpellState ProcessDamageAction(
        DamageActionResource damageAction,
        IGameStateData gameState);
}
```

#### StatusEffectLogicManager (Static)
Handles status effect business logic:

```csharp
public static class StatusEffectLogicManager
{
    public static StatusEffectsState ApplyStatusEffect(
        StatusEffectsState currentState,
        StatusEffectResource effect,
        int stacks,
        StatusEffectActionType actionType);
    
    public static StatusEffectsState TriggerEffects(
        StatusEffectsState currentState,
        StatusEffectTrigger trigger);
    
    public static StatusEffectsState ProcessDecay(
        StatusEffectsState currentState,
        StatusEffectDecayMode decayMode);
    
    public static int GetStacksOfEffect(
        StatusEffectsState currentState,
        StatusEffectType effectType);
}
```

### Updated Resource System

#### ActionResource System
ActionResource classes will be modified to work with the new state-based system:

```csharp
public abstract partial class ActionResource : Resource
{
    public abstract Color PopUpEffectColor { get; }
    public abstract string GetPopUpEffectText(IGameStateData gameState);
    public abstract GameCommand CreateExecutionCommand(string cardId);
}
```

Each action type will create appropriate commands:
- `DamageActionResource` → `ExecuteCardActionCommand` with damage logic
- `ModifierActionResource` → `AddSpellModifierCommand`
- `StatusEffectActionResource` → `ApplyStatusEffectCommand`

#### SpellCardResource Updates
SpellCardResource will be updated to work with the new command system:

```csharp
public abstract partial class SpellCardResource : Resource
{
    // Existing properties remain the same
    [Export] public Array<ActionResource> Actions { get; set; }
    
    // Updated execution method
    public IEnumerable<GameCommand> CreateExecutionCommands(string cardId)
    {
        return Actions.Select(action => action.CreateExecutionCommand(cardId));
    }
}
```

#### StatusEffectResource Integration
StatusEffectResource will be updated to work with the centralized state system and may need modifications to integrate with the new StatusEffectsState structure.

### Visual System Integration

Card visuals will subscribe to state changes and create popup effects based on state updates:

```csharp
public partial class Card : Control
{
    private void OnGameStateChanged(IGameStateData previousState, IGameStateData newState)
    {
        // Check for spell state changes affecting this card
        if (ShouldShowPopupEffect(previousState, newState))
        {
            CreatePopupEffect(GetEffectDataFromState(newState));
        }
    }
    
    private bool ShouldShowPopupEffect(IGameStateData previousState, IGameStateData newState)
    {
        // Logic to determine if this card should show effects based on state changes
    }
}
```

## Data Models

### State Transition Flow

1. **Spell Initiation**: `PlayHandCommand` → `StartSpellCommand`
   - Creates initial SpellState with empty modifiers and properties
   - Sets IsActive = true, records StartTime

2. **Action Processing**: For each card action → `ExecuteCardActionCommand`
   - Delegates to appropriate logic managers
   - Updates spell state with results
   - Triggers visual updates through state changes

3. **Spell Completion**: `CompleteSpellCommand`
   - Moves spell context to history
   - Clears active spell state
   - Processes any end-of-spell effects

### Status Effect Integration

Status effects will be processed through commands at appropriate trigger points:
- Turn start/end: `ProcessStatusEffectDecayCommand`
- Spell casting: `TriggerStatusEffectsCommand` with OnSpellCast trigger
- Damage dealt: `TriggerStatusEffectsCommand` with OnDamageDealt trigger

## Error Handling

### State Validation
- All state updates must pass validation before being applied
- Invalid state transitions will be rejected with clear error messages
- State consistency checks will prevent orphaned or inconsistent data

### Command Error Recovery
- Failed commands will not modify state
- Error information will be logged and optionally stored in spell history
- Visual feedback will be provided for any user-visible errors

### Rollback Capabilities
- Commands that fail partway through execution will not leave partial state changes
- All state modifications within a command are atomic

## Testing Strategy

### Unit Testing
- **Manager Classes**: Test all static logic functions with various input combinations
- **State Classes**: Test immutability, validation, and state transitions
- **Commands**: Test execution logic, validation, and error handling

### Integration Testing
- **Command Chains**: Test complete spell casting flows through multiple commands
- **State Consistency**: Verify state remains valid throughout complex operations
- **Visual Integration**: Test that state changes properly trigger visual updates

### Performance Testing
- **State Update Performance**: Measure impact of immutable state updates
- **Command Processing**: Ensure command queue processing doesn't introduce delays
- **Memory Usage**: Monitor memory usage of state history and ensure proper cleanup

### Regression Testing
- **Gameplay Preservation**: Verify identical behavior to current implementation
- **Visual Effects**: Ensure all popup effects and animations work identically
- **Status Effect Behavior**: Confirm status effects trigger and decay as expected

## Migration Strategy

### Phase 1: State Structure
1. Create new state classes (SpellState, StatusEffectsState)
2. Extend GameState with new properties
3. Update GameState validation logic

### Phase 2: Manager Classes
1. Implement SpellLogicManager with current SpellContext logic
2. Implement StatusEffectLogicManager with current StatusEffectManager logic
3. Create comprehensive unit tests for manager functions

### Phase 3: Commands
1. Implement spell-related commands
2. Implement status effect commands
3. Update SpellCastCommand to use new command chain

### Phase 4: Resource System Updates
1. Modify ActionResource base class and implementations
2. Update SpellCardResource to work with command system
3. Update StatusEffectResource for state integration
4. Remove SpellContext dependencies from all resources
5. Update any other resources that require refitting for the new system

### Phase 5: Visual Integration
1. Update card visuals to respond to state changes
2. Move popup effect creation from SpellProcessingManager to card visuals
3. Remove direct visual method calls

### Phase 6: Cleanup
1. Remove SpellContext class and all references
2. Remove old StatusEffectManager local state management
3. Remove SpellProcessingManager direct execution logic
4. Clean up unused imports and methods

Each phase will be completed and tested before proceeding to the next, ensuring the system remains functional throughout the migration.
### Adapt
ive Implementation Approach

During implementation, if any areas of the codebase require refitting or updates to maintain consistency with the new system, these will be identified and addressed as part of the implementation tasks. This includes:

- **Resource Dependencies**: Any resources that depend on the old SpellContext or StatusEffectManager systems
- **Interface Updates**: Interfaces that need modification to work with the new state-based approach
- **Service Integrations**: Services or managers that interact with spell or status effect systems
- **Event System Updates**: Any event-based communications that need to be updated for the new architecture
- **Validation Logic**: State validation that needs to account for the new spell and status effect state sections

The implementation will ensure a coherent, consistent, and functional system that follows game development standards and completely fulfills all requirements without leaving any legacy code or partial migrations.