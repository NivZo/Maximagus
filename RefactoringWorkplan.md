# Refactoring Workplan - Maximagus Architecture Transformation

## Overview
This workplan details the step-by-step transformation from the current fragmented architecture to a clean, maintainable MVC architecture with single source of truth state management.

## Phase 1: Foundation - Core Architecture (Days 1-4)

### 1.1 Command System Implementation
**Files to Create:**
- `Scripts/Commands/IGameCommand.cs` - Base command interface
- `Scripts/Commands/GameCommandProcessor.cs` - Central command processor
- `Scripts/Commands/CommandHistory.cs` - Undo/redo functionality
- `Scripts/Commands/Card/SelectCardCommand.cs`
- `Scripts/Commands/Card/DeselectCardCommand.cs` 
- `Scripts/Commands/Hand/PlayHandCommand.cs`
- `Scripts/Commands/Hand/DiscardHandCommand.cs`
- `Scripts/Commands/Hand/ReorderCardsCommand.cs`

**Implementation Details:**
```csharp
public interface IGameCommand
{
    bool CanExecute(IGameState currentState);
    IGameState Execute(IGameState currentState);
    IGameCommand CreateUndoCommand(IGameState previousState);
}
```

### 1.2 Single Source of Truth - GameState
**Files to Create:**
- `Scripts/State/GameState.cs` - Immutable game state
- `Scripts/State/GameStateBuilder.cs` - Builder for state construction
- `Scripts/State/HandState.cs` - Hand-specific state
- `Scripts/State/CardState.cs` - Individual card state
- `Scripts/State/PlayerState.cs` - Player stats and resources
- `Scripts/State/GamePhaseState.cs` - Current game phase

**Key Features:**
- Immutable state objects
- Builder pattern for complex updates
- State validation at construction
- Automatic event publishing on changes

### 1.3 Enhanced Event System
**Files to Modify:**
- `Scripts/Events/StateEvents.cs` - New state change events
- `Scripts/Events/GameStateEvents.cs` - Enhanced with proper event data

**Files to Create:**
- `Scripts/Events/CommandEvents.cs` - Command execution events
- `Scripts/Events/UIEvents.cs` - Pure UI interaction events

## Phase 2: Input Layer Refactoring (Days 5-7)

### 2.1 Unified Input System
**Files to Create:**
- `Scripts/Input/InputToCommandMapper.cs` - Convert inputs to commands
- `Scripts/Input/CardInputHandler.cs` - Card-specific input handling
- `Scripts/Input/KeyboardInputHandler.cs` - Keyboard input processing
- `Scripts/Input/MouseInputHandler.cs` - Mouse input processing

**Files to Modify:**
- `Scripts/Implementations/Managers/GameInputManager.cs` - Simplify to use mapper

### 2.2 Remove Input Logic from CardLogic
**Files to Heavily Refactor:**
- `Scripts/Implementations/Card/CardLogic.cs` - Remove all input handling
- `Scripts/Implementations/Card/Card.cs` - Simplify to pure view component

**New Approach:**
- Input handling moves to dedicated input handlers
- CardLogic becomes pure view controller
- All user actions convert to commands

## Phase 3: State-Driven Components (Days 8-11)

### 3.1 Card System Refactoring
**Files to Create:**
- `Scripts/Controllers/CardController.cs` - React to card state changes
- `Scripts/Controllers/HandController.cs` - React to hand state changes
- `Scripts/Views/CardView.cs` - Pure visual component

**Files to Heavily Refactor:**
- `Scripts/Implementations/Card/CardLogic.cs` - Transform to CardController
- `Scripts/Implementations/Card/CardVisual.cs` - Transform to CardView
- `Scripts/Implementations/Hand.cs` - Transform to HandView

### 3.2 Hand Management Refactoring
**Files to Create:**
- `Scripts/Services/HandService.cs` - Business logic for hand operations
- `Scripts/Controllers/HandViewController.cs` - Visual arrangement controller

**Files to Remove:**
- `Scripts/Implementations/Managers/HandManager.cs` - Logic moves to HandService

### 3.3 Manager Cleanup
**Files to Heavily Refactor:**
- `Scripts/Implementations/Managers/GameStateManager.cs` - Focus only on state machine
- `Scripts/Implementations/Managers/DragManager.cs` - Simplify to visual-only
- `Scripts/Implementations/Managers/HoverManager.cs` - Simplify to visual-only

## Phase 4: Scene File Updates (Days 12-13)

### 4.1 Card Scene Restructuring
**Files to Modify:**
- `Scenes/Card/Card.tscn` - Remove CardLogic node, add CardView
- `Scenes/Card/CardLogic.tscn` - Delete (functionality moves to controller)
- `Scenes/Card/CardVisual.tscn` - Rename to CardView.tscn

### 4.2 Hand Scene Updates
**Files to Modify:**
- `Scenes/Gameplay/Hand.tscn` - Update to use new controller system
- `Scenes/Main.tscn` - Update initialization flow

### 4.3 New Scene Components
**Files to Create:**
- `Scenes/Controllers/` - Directory for controller prefabs
- `Scenes/Controllers/CardController.tscn`
- `Scenes/Controllers/HandController.tscn`

## Phase 5: Service Layer Implementation (Days 14-15)

### 5.1 Replace Service Locator
**Files to Create:**
- `Scripts/DI/GameServiceContainer.cs` - Proper DI container
- `Scripts/DI/ServiceRegistration.cs` - Service registration

**Files to Remove:**
- `Scripts/Implementations/Infra/ServiceLocator.cs` - Replace with DI

### 5.2 Game Services
**Files to Create:**
- `Scripts/Services/GameStateService.cs` - State management service
- `Scripts/Services/CommandService.cs` - Command processing service
- `Scripts/Services/InputService.cs` - Input handling service

## Phase 6: Integration & Testing (Days 16-18)

### 6.1 System Integration
- Wire up all new components
- Ensure proper event flow
- Test command execution paths
- Validate state consistency

### 6.2 Behavior Preservation
- Ensure all existing gameplay works
- Maintain visual behaviors
- Preserve card animations and effects
- Test drag and drop functionality

### 6.3 Performance Validation
- Ensure no performance regression
- Optimize command processing
- Validate memory usage
- Test with large hand sizes

## Implementation Strategy

### Migration Approach: **Big Bang with Feature Flags**
1. Implement new architecture alongside old
2. Use feature flags to switch between systems
3. Migrate functionality piece by piece
4. Remove old code once validated

### Risk Mitigation
1. **Backup Strategy**: Full project backup before major changes
2. **Incremental Testing**: Test each phase thoroughly
3. **Rollback Plan**: Ability to revert to previous architecture
4. **Documentation**: Comprehensive change documentation

### Dependencies Between Phases
- Phase 1 must complete before Phase 2
- Phase 2 can partially overlap with Phase 3
- Phase 4 depends on completion of Phase 3
- Phase 5 can run parallel to Phase 4
- Phase 6 requires all previous phases

## Success Criteria

### Functional Requirements
- [ ] All existing gameplay features work identically
- [ ] Card selection, drag/drop, and hand management preserved
- [ ] Visual effects and animations maintained
- [ ] Performance equivalent or better

### Architectural Requirements
- [ ] Single source of truth for all game state
- [ ] All user actions go through command system
- [ ] Clean separation between views and logic
- [ ] Event flow follows: State Change → Event → Controller → View
- [ ] No direct node-to-node dependencies

### Quality Requirements
- [ ] Code is more maintainable and readable
- [ ] New features can be added easily
- [ ] Unit testing is possible for game logic
- [ ] Debugging is straightforward with clear data flow

## Estimated Timeline: 18 Days
- **Phase 1**: 4 days (Foundation)
- **Phase 2**: 3 days (Input Layer)
- **Phase 3**: 4 days (Components)
- **Phase 4**: 2 days (Scenes)
- **Phase 5**: 2 days (Services)
- **Phase 6**: 3 days (Integration)

## Next Steps
1. Review and approve this workplan
2. Set up development environment for parallel development
3. Begin Phase 1 implementation
4. Establish testing strategy for each phase