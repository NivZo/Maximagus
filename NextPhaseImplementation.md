# Next Phase Implementation Plan

## ✅ COMPLETED IN THIS PHASE

### **1. Card Selection Commands Integration**
- **STATUS**: ✅ IMPLEMENTED
- **Changes Made**:
  - Modified `CardLogic.HandleClick()` to use `SelectCardCommand`/`DeselectCardCommand`
  - Updated commands to call `CardLogic.SetSelected()` directly
  - Added fallback to legacy system if CommandProcessor unavailable
  - Fixed circular dependency between CardLogic and Commands

### **2. Legacy GameStateManager Cleanup**
- **STATUS**: ✅ COMPLETED
- **Files Removed**:
  - `Scripts/Implementations/Managers/GameStateManager.cs` (legacy)
  - `Scripts/Interfaces/Managers/IGameStateManager.cs` (legacy)
  - `Scripts/Implementations/Turn/*` (entire legacy state machine)
  - `Scripts/Interfaces/Turn/*` (legacy interfaces)
  - `Scripts/Events/GameStateEvents.cs` (legacy events)

### **3. Unified Command Architecture**
- **STATUS**: ✅ WORKING
- **Architecture**: Single command system handles all interactions
- **Flow**: Input → InputToCommandMapper → GameCommandProcessor → Commands → GameState

## ⚠️ PARTIALLY IMPLEMENTED (READY FOR INTEGRATION)

### **4. Card Drag/Reorder Commands**
- **STATUS**: ⚠️ INFRASTRUCTURE READY, INTEGRATION PENDING
- **Current State**:
  - `CardInputHandler` captures drag events ✅
  - `InputToCommandMapper` maps to `ReorderCardsCommand` ✅
  - `ReorderCardsCommand` exists and functional ✅
  - `CardLogic.StopDragging()` still uses legacy system ❌

- **NEXT PHASE TASK**:
  ```csharp
  // In CardLogic.StopDragging() - Replace:
  _eventBus?.Publish(new CardDragEndedEvent(card));
  
  // With:
  if (_commandProcessor != null)
  {
      // Determine new card order from drag operation
      var newOrder = DetermineDragOrder(); // Implementation needed
      var command = new ReorderCardsCommand(newOrder);
      _commandProcessor.ExecuteCommand(command);
  }
  ```

## 🔄 REQUIRES FUTURE PHASES

### **5. Mouse-to-Command Integration**
- **STATUS**: 🔄 REQUIRES ARCHITECTURAL CHANGES
- **Issue**: Card interactions bypass InputToCommandMapper
- **Current Flow**: 
  ```
  Card Mouse Click → CardLogic.HandleClick() → SelectCardCommand (Direct)
  ```
- **Target Flow**:
  ```
  Card Mouse Click → CardInputHandler → InputToCommandMapper → GameCommandProcessor → SelectCardCommand
  ```
- **Complexity**: HIGH - Requires removing duplicate input handling

### **6. Advanced State Synchronization**
- **STATUS**: 🔄 REQUIRES DESIGN PHASE
- **Issue**: GameState and real objects can get out of sync
- **Need**: Bidirectional state synchronization system
- **Complexity**: HIGH - Core architecture change

### **7. Complete Legacy Removal**
- **STATUS**: 🔄 REQUIRES GRADUAL MIGRATION
- **Remaining Legacy Systems**:
  - Event-based card interactions (CardClickedEvent, etc.)
  - Direct card manipulation in Hand.cs
  - Legacy drag/hover managers
- **Approach**: Replace system by system

## 📝 IMMEDIATE TASKS FOR NEXT SESSION

### **Priority 1: Complete Card Reordering**
1. Implement drag order detection in `CardLogic.StopDragging()`
2. Replace legacy drag event with `ReorderCardsCommand`
3. Test drag-and-drop with command system

### **Priority 2: Clean Up Input Duplication**
1. Remove direct mouse handling from CardLogic
2. Route all card interactions through CardInputHandler
3. Consolidate input processing

### **Priority 3: Legacy Event Cleanup**
1. Remove unused card events (CardClickedEvent, etc.)
2. Remove legacy hover/drag managers if no longer needed
3. Simplify event system

## 🎯 SUCCESS METRICS

### **Current Status**: 
- ✅ Keyboard commands work (Play, Discard, Start Game)
- ✅ Card selection works via mouse (through command system)
- ✅ No GameStateManager conflicts
- ✅ Build succeeds with no errors
- ⚠️ Card dragging works but uses legacy system

### **Next Phase Goals**:
- ✅ Card reordering through command system  
- ✅ Unified input processing
- ✅ Reduced legacy code footprint
- ✅ Professional error handling throughout

## 🔧 TECHNICAL NOTES

### **Command Integration Pattern**:
```csharp
// ESTABLISHED PATTERN (Card Selection):
if (_commandProcessor != null)
{
    var command = IsSelected ? new DeselectCardCommand(cardId) : new SelectCardCommand(cardId);
    var success = _commandProcessor.ExecuteCommand(command);
    // Update local state based on command result
}
else
{
    // Fallback to legacy system
}
```

### **Service Integration Pattern**:
```csharp
// ESTABLISHED PATTERN (Service Access):
private void SetupServices()
{
    _commandProcessor = ServiceLocator.GetService<GameCommandProcessor>();
    // Other services...
}
```

This foundation enables rapid implementation of remaining command integrations in future phases.