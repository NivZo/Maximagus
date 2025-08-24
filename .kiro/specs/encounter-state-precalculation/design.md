# Design Document

## Overview

This design introduces an EncounterState that unifies SpellState and StatusEffectsState management, and enhances the pre-calculation system to create complete state snapshots. The current pre-calculation system only calculates damage values in advance but doesn't account for status effect stacks and other state changes that affect damage calculations. This enhancement will create comprehensive EncounterState snapshots that include all state changes, enabling accurate pre-calculation of action results including status effect interactions.

The key architectural changes involve creating an EncounterState abstraction that encapsulates both spell and status effect state, extending the pre-calculation system to generate complete state snapshots for each action, and updating action execution to use these snapshots for consistent state application.

## Architecture

### EncounterState Structure

The EncounterState will serve as a unified container for all encounter-related state:

```csharp
public class EncounterState
{
    public SpellState Spell { get; }
    public StatusEffectsState StatusEffects { get; }
    public DateTime Timestamp { get; }
    public int ActionIndex { get; }

    public EncounterState(
        SpellState spell,
        StatusEffectsState statusEffects,
        DateTime timestamp,
        int actionIndex = 0)
    {
        Spell = spell ?? throw new ArgumentNullException(nameof(spell));
        StatusEffects = statusEffects ?? throw new ArgumentNullException(nameof(statusEffects));
        Timestamp = timestamp;
        ActionIndex = actionIndex;
    }

    // Immutable update methods
    public EncounterState WithSpell(SpellState newSpell);
    public EncounterState WithStatusEffects(StatusEffectsState newStatusEffects);
    public EncounterState WithTimestamp(DateTime newTimestamp);
    public EncounterState WithActionIndex(int newActionIndex);
    public EncounterState WithBoth(SpellState newSpell, StatusEffectsState newStatusEffects);
    
    // Validation and utility methods
    public bool IsValid();
    public static EncounterState FromGameState(IGameStateData gameState, DateTime timestamp);
    public IGameStateData ApplyToGameState(IGameStateData gameState);
}
```

### Enhanced Pre-Calculation System

#### EncounterStateSnapshot
```csharp
public class EncounterStateSnapshot
{
    public string ActionKey { get; }
    public EncounterState ResultingState { get; }
    public ActionExecutionResult ActionResult { get; }
    public DateTime CreatedAt { get; }

    public EncounterStateSnapshot(
        string actionKey,
        EncounterState resultingState,
        ActionExecutionResult actionResult,
        DateTime createdAt)
    {
        ActionKey = actionKey ?? throw new ArgumentNullException(nameof(actionKey));
        ResultingState = resultingState ?? throw new ArgumentNullException(nameof(resultingState));
        ActionResult = actionResult ?? throw new ArgumentNullException(nameof(actionResult));
        CreatedAt = createdAt;
    }
}
```

#### Enhanced SpellLogicManager
The SpellLogicManager will be extended to work with EncounterState snapshots:

```csharp
public static class SpellLogicManager
{
    // New snapshot-based pre-calculation methods
    public static EncounterStateSnapshot PreCalculateActionWithSnapshot(
        ActionResource action,
        EncounterState currentEncounterState);
    
    public static ImmutableArray<EncounterStateSnapshot> PreCalculateSpellWithSnapshots(
        IGameStateData initialGameState,
        IEnumerable<CardState> playedCards);
    
    // Enhanced existing methods to work with EncounterState
    public static ActionExecutionResult PreCalculateActionResult(
        ActionResource action,
        EncounterState encounterState);
    
    public static (float finalDamage, ImmutableArray<ModifierData> remainingModifiers) 
        ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState);
    
    // Snapshot application methods
    public static IGameStateData ApplyEncounterSnapshot(
        IGameStateData gameState,
        EncounterStateSnapshot snapshot);
}
```

### Snapshot Management System

#### EncounterSnapshotManager
A new manager class to handle snapshot storage and retrieval:

```csharp
public static class EncounterSnapshotManager
{
    public static void StoreSnapshots(
        string spellId,
        ImmutableArray<EncounterStateSnapshot> snapshots);
    
    public static EncounterStateSnapshot GetSnapshotForAction(
        string spellId,
        string actionKey);
    
    public static ImmutableArray<EncounterStateSnapshot> GetAllSnapshots(string spellId);
    
    public static void ClearSnapshots(string spellId);
    
    public static void ClearExpiredSnapshots(TimeSpan maxAge);
}
```

## Components and Interfaces

### Updated GameState Integration

The IGameStateData interface will be extended with EncounterState convenience methods:

```csharp
public static class GameStateExtensions
{
    public static EncounterState GetEncounterState(this IGameStateData gameState)
    {
        return EncounterState.FromGameState(gameState, DateTime.UtcNow);
    }
    
    public static IGameStateData WithEncounterState(
        this IGameStateData gameState,
        EncounterState encounterState)
    {
        return gameState
            .WithSpell(encounterState.Spell)
            .WithStatusEffects(encounterState.StatusEffects);
    }
}
```

### Enhanced Command System

#### New Commands
- `PreCalculateSpellCommand` - Creates complete EncounterState snapshots for all actions
- `ApplyEncounterSnapshotCommand` - Applies a pre-calculated snapshot to game state

#### Updated Commands
Existing commands will be updated to work with the new snapshot system:

```csharp
public class ExecuteCardActionCommand : GameCommand
{
    // Updated to use snapshots
    protected override CommandResult ExecuteInternal(IGameStateData gameState)
    {
        // Fetch pre-calculated snapshot
        var snapshot = EncounterSnapshotManager.GetSnapshotForAction(spellId, actionKey);
        
        if (snapshot == null)
        {
            throw new InvalidOperationException(
                $"No pre-calculated snapshot found for action {actionKey}");
        }
        
        // Apply snapshot to game state
        var newGameState = SpellLogicManager.ApplyEncounterSnapshot(gameState, snapshot);
        
        return CommandResult.Success(newGameState);
    }
}
```

### Status Effect Integration

The StatusEffectLogicManager will be enhanced to work with EncounterState:

```csharp
public static class StatusEffectLogicManager
{
    // New methods that work with EncounterState
    public static EncounterState ApplyStatusEffectToEncounter(
        EncounterState currentState,
        StatusEffectResource effect,
        int stacks,
        StatusEffectActionType actionType);
    
    public static EncounterState TriggerEffectsInEncounter(
        EncounterState currentState,
        StatusEffectTrigger trigger);
    
    public static EncounterState ProcessDecayInEncounter(
        EncounterState currentState,
        StatusEffectDecayMode decayMode);
    
    // Existing methods adapted for EncounterState
    public static StatusEffectsState ApplyStatusEffect(
        StatusEffectsState currentState,
        StatusEffectResource effect,
        int stacks,
        StatusEffectActionType actionType);
    
    // ... other existing methods remain the same
}
```

## Data Models

### Pre-Calculation Flow with Snapshots

1. **Spell Initiation**: `PreCalculateSpellCommand` is executed
   - Creates initial EncounterState from current game state
   - Iterates through all actions in the spell sequence
   - For each action, creates a snapshot of the resulting EncounterState

2. **Action Pre-Calculation**: For each action
   - Uses previous action's snapshot as base state (or initial state for first action)
   - Calculates action result including status effect interactions
   - Simulates all state changes (spell properties, modifiers, status effects)
   - Creates EncounterStateSnapshot with complete resulting state
   - Stores snapshot with timestamp-based key

3. **Action Execution**: During live spell execution
   - `ExecuteCardActionCommand` fetches pre-calculated snapshot
   - Applies snapshot's EncounterState to current game state
   - Updates both spell state and status effect state atomically
   - Triggers visual updates based on state changes

### Snapshot Storage Strategy

Snapshots will be stored in memory with the following structure:
- **Key**: `{spellId}_{actionKey}` where actionKey is the action's unique identifier
- **Value**: EncounterStateSnapshot containing complete state and action result
- **Cleanup**: Snapshots are cleared when spell completes or is cancelled
- **Memory Management**: Automatic cleanup of expired snapshots to prevent memory leaks

### State Consistency Guarantees

1. **Atomic Updates**: EncounterState changes are applied atomically to both spell and status effect state
2. **Validation**: All EncounterState instances are validated for consistency
3. **Immutability**: EncounterState is immutable, preventing accidental modifications
4. **Timestamp Tracking**: Each snapshot includes creation timestamp for debugging and cleanup

## Error Handling

### Snapshot Validation
- All EncounterState instances must pass validation before being stored
- Invalid snapshots will be rejected with detailed error messages
- Snapshot consistency checks ensure spell and status effect state alignment

### Missing Snapshot Handling
- If a required snapshot is missing during execution, the system will throw a clear error
- Error messages will indicate which action and spell are affected
- Fallback mechanisms will not be provided to ensure pre-calculation consistency

### Memory Management
- Automatic cleanup of expired snapshots prevents memory leaks
- Configurable retention policies for snapshot storage
- Monitoring and alerting for excessive snapshot memory usage

## Testing Strategy

### Unit Testing
- **EncounterState**: Test immutability, validation, and state transitions
- **Snapshot Creation**: Test snapshot generation for various action types
- **Snapshot Application**: Test applying snapshots to game state
- **Manager Functions**: Test all enhanced logic manager functions

### Integration Testing
- **Complete Pre-Calculation Flow**: Test full spell pre-calculation with snapshots
- **Snapshot-Based Execution**: Test executing spells using pre-calculated snapshots
- **Status Effect Integration**: Test status effects in snapshot-based system
- **Memory Management**: Test snapshot cleanup and memory usage

### Performance Testing
- **Snapshot Creation Performance**: Measure time to create snapshots for complex spells
- **Snapshot Storage**: Test memory usage and retrieval performance
- **State Application**: Measure performance of applying snapshots to game state

### Regression Testing
- **Calculation Accuracy**: Verify identical results between old and new systems
- **Visual Effects**: Ensure visual effects work correctly with snapshot-based updates
- **Status Effect Behavior**: Confirm status effects behave identically

## Migration Strategy

### Phase 1: EncounterState Infrastructure
1. Create EncounterState class with validation and utility methods
2. Create EncounterStateSnapshot class for snapshot storage
3. Create EncounterSnapshotManager for snapshot management
4. Add GameState extension methods for EncounterState integration
5. Write comprehensive unit tests for new classes

### Phase 2: Enhanced Pre-Calculation System
1. Update SpellLogicManager to work with EncounterState
2. Implement snapshot-based pre-calculation methods
3. Create PreCalculateSpellCommand for generating snapshots
4. Update existing pre-calculation to use EncounterState internally
5. Write integration tests for snapshot generation

### Phase 3: Snapshot-Based Execution
1. Update ExecuteCardActionCommand to use snapshots
2. Implement ApplyEncounterSnapshotCommand
3. Update spell execution flow to use snapshot-based approach
4. Remove legacy pre-calculation result storage from SpellState
5. Write integration tests for snapshot-based execution

### Phase 4: Status Effect Integration
1. Update StatusEffectLogicManager to work with EncounterState
2. Ensure status effect calculations are included in snapshots
3. Update status effect commands to work with snapshot system
4. Test status effect interactions in snapshot-based system

### Phase 5: SOLID Principles Compliance Review
1. Review all new and modified classes for SOLID principles compliance
2. Refactor any existing infrastructure that violates SOLID principles
3. Ensure proper separation of concerns and dependency management
4. Update interfaces to follow Interface Segregation Principle

### Phase 6: Legacy System Removal
1. Remove old damage-only pre-calculation methods
2. Remove ActionExecutionResult storage from SpellState
3. Clean up unused pre-calculation infrastructure
4. Update all references to use new EncounterState-based system

### Phase 7: Performance Optimization and Cleanup
1. Optimize snapshot creation and storage performance
2. Implement memory management and cleanup policies
3. Add monitoring and debugging tools for snapshot system
4. Perform final code review and cleanup

Each phase will be completed and tested before proceeding to ensure system stability throughout the migration. The implementation will prioritize core EncounterState functionality before addressing infrastructure improvements, maintaining focus on the primary objective while ensuring SOLID principles compliance.