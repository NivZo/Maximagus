# Implementation Prompt - Maximagus Architecture Refactoring

## Mission Statement
You are a senior software architect tasked with implementing a comprehensive refactoring of the Maximagus card game from a fragmented, tightly-coupled architecture to a clean, maintainable MVC architecture with single source of truth state management.

## Context & Prerequisites

### Project Understanding Required
1. **Read Memory Bank First**: Start by reading all files in `C:/Users/nivzohar/Documents/Cline/MemoryBanks/Maximagus/Maximagus/` to understand the target architecture
2. **Review Analysis Documents**: Study `ArchitecturalReview.md`, `RefactoringWorkplan.md`, and `WorkplanReview.md` thoroughly
3. **Current Architecture**: Understand that the existing codebase has severe architectural violations requiring complete restructuring

### Key Architectural Goals
- **Single Source of Truth**: All game state centralized in immutable GameState objects
- **Command Pattern**: All user inputs must flow through validated commands
- **MVC Separation**: Clear boundaries between Model (State), View (Godot Nodes), Controller (Event Handlers)
- **Event-Driven Updates**: State changes trigger events, controllers update views
- **Zero User Experience Impact**: All existing functionality and visual behaviors must be preserved

## Implementation Strategy: Phase-by-Phase Execution

### Phase 1: Foundation (Days 1-5) - CRITICAL SUCCESS PHASE
**Objective**: Establish core architecture without breaking existing functionality

#### 1.1 Command System Implementation
**MUST CREATE** these exact files:
```
Scripts/Commands/IGameCommand.cs
Scripts/Commands/GameCommandProcessor.cs  
Scripts/Commands/CommandHistory.cs
Scripts/Commands/Card/SelectCardCommand.cs
Scripts/Commands/Card/DeselectCardCommand.cs
Scripts/Commands/Hand/PlayHandCommand.cs
Scripts/Commands/Hand/DiscardHandCommand.cs
Scripts/Commands/Hand/ReorderCardsCommand.cs
```

**CRITICAL IMPLEMENTATION DETAILS**:
```csharp
public interface IGameCommand 
{
    bool CanExecute(IGameState currentState);
    IGameState Execute(IGameState currentState);  
    IGameCommand CreateUndoCommand(IGameState previousState);
    string GetDescription(); // For debugging
}
```

**PITFALL WARNING**: Commands must be **completely immutable** and **side-effect free**. They should only return new state objects, never modify existing ones.

#### 1.2 GameState Implementation
**MUST CREATE** these files:
```
Scripts/State/GameState.cs - Main immutable state
Scripts/State/GameStateBuilder.cs - Builder pattern
Scripts/State/HandState.cs - Hand-specific state  
Scripts/State/CardState.cs - Individual card state
Scripts/State/PlayerState.cs - Player stats
Scripts/State/GamePhaseState.cs - Current phase
```

**CRITICAL DESIGN PATTERN**:
```csharp
public class GameState : IGameState
{
    public HandState Hand { get; }
    public PlayerState Player { get; }
    public GamePhaseState Phase { get; }
    
    // MUST be immutable - no setters!
    private GameState(HandState hand, PlayerState player, GamePhaseState phase)
    {
        Hand = hand;
        Player = player;  
        Phase = phase;
    }
}
```

**SUCCESS METRIC**: Commands can create new GameState objects without compilation errors.

#### 1.3 Validation System
**MUST CREATE**:
```
Scripts/Validation/CommandValidator.cs
Scripts/Validation/StateValidator.cs
Scripts/Validation/BusinessRules.cs  
```

**PITFALL WARNING**: Validation must happen **before** command execution, not during state construction.

### Phase 2: Input Layer (Days 6-8.5)

#### 2.1 Unified Input System
**MUST CREATE**:
```
Scripts/Input/InputToCommandMapper.cs
Scripts/Input/CardInputHandler.cs
Scripts/Input/KeyboardInputHandler.cs
Scripts/Input/MouseInputHandler.cs
```

**CRITICAL SUCCESS PATTERN**:
```csharp
// ALL input must follow this flow:
User Action → InputHandler → InputToCommandMapper → GameCommand → CommandProcessor → GameState
```

**PITFALL WARNING**: Do NOT modify existing CardLogic input handling until Phase 3. Use feature flags to switch between old and new input systems.

#### 2.2 Feature Flag Implementation
**MUST IMPLEMENT** feature flag system:
```csharp
public static class FeatureFlags 
{
    public static bool UseNewInputSystem = false;
    public static bool UseNewStateSystem = false;
    public static bool UseNewCardSystem = false;
}
```

**SUCCESS METRIC**: Can switch between old and new input systems without breaking functionality.

### Phase 3: Component Refactoring (Days 9-13) - HIGHEST RISK PHASE

#### 3.1 Card System Transformation
**CHALLENGE**: This is the most complex phase. Card logic is currently spread across 5+ files.

**APPROACH**: Create new components alongside existing ones, then gradually migrate:

1. **Create CardController.cs** - Handles state events, no input
2. **Create CardView.cs** - Pure visual component
3. **Modify CardLogic.cs** - Remove input handling gradually
4. **Use feature flags** to switch between old/new card systems

**CRITICAL PITFALL**: Do NOT remove existing CardLogic until new system is 100% functional.

**SUCCESS METRICS**: 
- Card selection works identically with new system
- Drag and drop behavior preserved
- All card animations work correctly
- No visual glitches or timing issues

#### 3.2 Hand System Refactoring
**TRANSFORMATION**:
- `Hand.cs` → `HandView.cs` (visual only)
- `HandManager.cs` → `HandService.cs` (business logic only)
- Create `HandController.cs` (event handling)

**CRITICAL REQUIREMENT**: Fan arrangement and card positioning must work identically.

### Phase 4: Scene File Updates (Days 14-15)

#### 4.1 Scene Restructuring
**BEFORE** touching any `.tscn` files, ensure Phase 3 is 100% complete and tested.

**Card.tscn transformation**:
```
OLD: Card -> CardVisual + CardLogic
NEW: Card -> CardView + CardInputArea (Area2D only)
```

**PITFALL WARNING**: Scene file corruption is hard to recover from. Make full backups before any scene modifications.

**SUCCESS METRIC**: All scene files load without errors and maintain identical visual appearance.

### Phase 5: Service Layer (Days 16-17)

#### 5.1 Replace Service Locator
**ONLY** do this after all other phases are complete and stable.

Create proper dependency injection:
```
Scripts/DI/GameServiceContainer.cs
Scripts/DI/ServiceRegistration.cs
```

**PITFALL WARNING**: Service Locator removal affects every class. This must be the last major change.

### Phase 6: Integration & Validation (Days 18-20.5)

#### 6.1 Comprehensive Testing
**MUST VERIFY**:
- All existing gameplay features work identically
- Performance is equivalent or better
- No memory leaks or performance regressions
- Visual effects and animations preserved
- Card drag/drop feels identical
- Hand management behaves the same

**SUCCESS CRITERIA CHECKLIST**:
- [ ] Card selection visual feedback works
- [ ] Drag and drop feels responsive
- [ ] Hand fan arrangement is identical  
- [ ] Keyboard shortcuts work (Enter, Space, Delete)
- [ ] Game state transitions work correctly
- [ ] No console errors or warnings
- [ ] Memory usage is stable
- [ ] Frame rate is maintained

## Critical Pitfalls & How to Avoid Them

### 1. Premature Old Code Removal
**DANGER**: Removing existing code before new system is proven.
**SOLUTION**: Keep old and new systems running in parallel with feature flags.

### 2. State Mutation Bugs
**DANGER**: Accidentally modifying immutable state objects.
**SOLUTION**: Use readonly fields, private constructors, and builder patterns.

### 3. Event Ordering Issues  
**DANGER**: Events firing in wrong order causing visual glitches.
**SOLUTION**: Ensure GameState publishes events AFTER state is fully updated.

### 4. Scene File Corruption
**DANGER**: Breaking .tscn files during restructuring.
**SOLUTION**: Full project backup before any scene modifications.

### 5. Performance Regression
**DANGER**: New architecture being slower than old one.
**SOLUTION**: Benchmark key operations (card selection, hand arrangement) before and after.

### 6. Input Responsiveness Loss
**DANGER**: New input system feeling less responsive.  
**SOLUTION**: Preserve exact timing and feedback loops from original implementation.

## Success Validation Protocol

### After Each Phase:
1. **Functionality Test**: All existing features work
2. **Performance Test**: No significant slowdown
3. **Visual Test**: All animations and effects preserved
4. **Integration Test**: New components work with existing ones

### Final Validation:
1. **User Experience Test**: Play through complete game session
2. **Performance Benchmark**: Compare to original performance
3. **Code Quality Review**: Verify architectural goals achieved
4. **Documentation**: All new patterns documented in memory bank

## Failure Recovery Plan

### If Phase Fails:
1. **Immediate Rollback**: Revert to last known good state
2. **Issue Analysis**: Identify specific failure point
3. **Targeted Fix**: Address root cause only
4. **Re-test**: Full validation before proceeding

### If Major Architecture Issue:
1. **Stop Implementation**: Don't proceed with broken foundation
2. **Architectural Review**: Re-evaluate approach
3. **Alternative Strategy**: Consider different implementation path
4. **Stakeholder Communication**: Inform about delays and issues

## Key Implementation Principles

### 1. Preserve User Experience
Every change must maintain identical user experience. If something feels different, it's wrong.

### 2. Fail Fast Philosophy  
If something doesn't work correctly, stop and fix it before proceeding.

### 3. Incremental Validation
Test extensively after each component is created.

### 4. Clear Rollback Points
Always have a working version to fall back to.

### 5. Documentation as You Go
Update memory bank with decisions and learnings during implementation.

## Final Notes

This refactoring is **critical** for the project's long-term sustainability. The current architecture is unmaintainable and prevents adding new features. However, it's also **high-risk** because it touches every major system.

**Success depends on**:
- Careful phase-by-phase execution
- Thorough testing at each step  
- Maintaining parallel old/new systems
- Never breaking existing user experience
- Having clear rollback plans

**Your mission is to transform this codebase into a clean, maintainable architecture while preserving every aspect of the current user experience.**