# Card System Refactor Summary

## Overview
Successfully unified the fragmented card system (CardVisual, CardLogic, and Card) into a single, state-driven Card component that implements all functionality in one file.

## Changes Made

### 1. Unified Card Component (`Scripts/Implementations/Card/Card.cs`)
- **Consolidated functionality**: Combined all visual, logic, and data management into one unified Card class
- **State-driven architecture**: Card properties (IsSelected, IsDragging, IsHovering) now read directly from game state
- **Event-driven updates**: Card responds to state changes through the event system rather than polling
- **Static Create method**: Simplified card instantiation with `Card.Create()` method that handles complete setup

### 2. Scene Structure Maintained (`Scenes/Card/Card.tscn`)
- **Same node hierarchy**: Preserved the original Card scene structure with Textures/Card/Art/Shadow nodes
- **Simplified setup**: Removed dependencies on separate CardVisual and CardLogic scenes
- **Direct configuration**: All visual properties now configured directly on the main Card node

### 3. Updated Dependencies
- **CardSlot.cs**: Updated to work directly with unified Card component (removed Card.Logic references)
- **Hand.cs**: Updated all references from Card.Logic and Card.Visual to direct Card properties
- **Removed obsolete files**: Deleted CardLogic.cs, CardVisual.cs, CardLogic.tscn, CardVisual.tscn

## Implementation Details

### State-Driven Properties
The new Card component reads state directly from the game state system:
```csharp
public bool IsSelected => GetCardStateFromGameState()?.IsSelected ?? false;
public bool IsDragging => GetCardStateFromGameState()?.IsDragging ?? false;
public bool IsHovering => _hoverManager?.CurrentlyHoveringCard == this;
```

### Event Flow (State-Affecting)
1. Card receives input event (mouse click, drag threshold)
2. Card sends GameCommand (SelectCardCommand, DeselectCardCommand, StartDragCommand, EndDragCommand)
3. GameCommand updates state
4. State change events published
5. Card reads updated state and visuals update naturally

### Visual Effects (Non-State-Affecting)
- Mouse enter/exit: Pure visual hover effects and animations
- Mouse movement: Perspective visual effects during hover
- Card movement: Smooth movement to target positions s  et by state

### Key Features
- **Separation of concerns**: Clear distinction between state-affecting commands and visual-only effects
- **Single responsibility**: One component handles all card functionality
- **Maintainable**: Easier to understand and modify card behavior
- **Performance**: Reduced overhead from multiple components

## Benefits Achieved

1. **Simplified Architecture**: One file instead of three separate components
2. **State-Driven**: Card behavior is completely driven by game state
3. **Maintainable**: Single source of truth for card functionality
4. **Consistent**: Unified approach to card management
5. **Debuggable**: Easier to trace card behavior and state changes

## Testing Status
- ✅ Project builds successfully without compilation errors
- ✅ All references updated across the codebase
- ✅ Obsolete files removed cleanly
- ✅ **CRITICAL BUG FIXED**: Event subscription memory leak resolved

## Critical Bug Fixes Applied

### Fix 1: Removed Unnecessary Event Subscription
**Issue**: Cards were subscribing to `HandCardSlotsChangedEvent` which is never published anywhere in the codebase.

**Root Cause**: Dead code - event subscription without corresponding publisher.

**Solution**: Removed the entire event subscription mechanism from Card component as it serves no purpose.

### Fix 2: Improved Hand State Comparison
**Issue**: Hash code comparison in Hand state change detection was unreliable, potentially causing excessive card recreation.

**Root Cause**: `GetHashCode()` comparison is not reliable for detecting actual state changes.

**Solution**: Implemented proper state comparison that checks:
- Card count differences
- Individual card state differences (CardId, IsSelected, IsDragging)

```csharp
// BEFORE (problematic):
if (currentState.Hand.GetHashCode() != _lastHandState?.GetHashCode())

// AFTER (reliable):
bool handStateChanged = _lastHandState == null ||
                       _lastHandState.Cards.Count != currentState.Hand.Cards.Count ||
                       !CardsAreEqual(_lastHandState.Cards, currentState.Hand.Cards);
```

## Regression Fixes Applied

After initial success with card unification, three regressions were identified and fixed:

### Fix 3: Smooth Dragging Restored
**Issue**: Stiff card dragging due to direct mouse following instead of interpolated movement.

**Solution**:
- Restored interpolated movement for all card positions (dragging and static)
- Use faster interpolation (15fps) when dragging vs normal movement (10fps)
- Mouse position calculated as target, card smoothly interpolates to follow

### Fix 4: ZIndex Reset After Drag
**Issue**: Card ZIndex values not being reset to hand order after drag operations.

**Solution**: Added deferred `AdjustFanEffect()` call after hover/drag ends to recalculate proper Z-indices.

### Fix 5: Centered Hover Scale Animation
**Issue**: Card scaling not centered due to missing pivot offset updates.

**Solution**: Update pivot offset to center before scaling operations in both hover and drag states.

## Major Architectural Improvement: CardSlot Elimination

### CardSlot Component Removal ✅
**Problem**: Unnecessary intermediate CardSlot component added complexity without benefit.
**Solution**:
- Made Card implement IOrderable directly
- Eliminated CardSlot.cs and CardSlot.tscn files
- Cards now work directly with OrderedContainer
- Simplified Hand scene structure (removed CardSlots node)

### State-Driven Card Ordering ✅
**Problem**: Card reordering was handled visually without updating game state.
**Solution**:
- Drag operations now send ReorderCardsCommand with new card order (CardId array)
- Command updates CardState.Position values in game state
- Hand detects position changes and visually reorders cards accordingly
- Complete state-driven architecture for card positioning

### Technical Improvements
1. **Simplified Architecture**:
   - Card → OrderedContainer (direct)
   - Eliminated Card → CardSlot → OrderedContainer indirection

2. **State-Driven Ordering**:
   - CardState already had Position property
   - Hand.SyncCardOrder() ensures visual order matches state positions
   - ReorderCardsCommand handles state updates

3. **Cleaner Code**:
   - Removed CardSlot dependencies from Card and Hand
   - Updated Hand scene to use CardsContainer instead of CardSlotsContainer
   - All legacy CardSlot code eliminated

## New ReorderCardsCommand with Position Updates ✅
**Problem**: Previous ReorderCardsCommand only reordered cards in the list without updating Position values.
**Solution**:
- Updated ReorderCardsCommand.Execute() to properly update CardState.Position values
- Each card's position now reflects its actual index in the reordered list
- Missing cards (not in reorder list) are appended with correct position indices
- Complete state-driven reordering that updates both order and position values

### Technical Implementation
```csharp
// For each card in new order, update its position to match index
for (int i = 0; i < _newCardOrder.Count; i++)
{
    var cardId = _newCardOrder[i];
    if (cardDict.TryGetValue(cardId, out var card))
    {
        var updatedCard = card.WithPosition(i);  // Position = index
        reorderedCards.Add(updatedCard);
    }
}
```

## Current Status
- ✅ **Core functionality**: 10 cards drawn, selectable, draggable, reorderable
- ✅ **Card duplication**: Completely resolved
- ✅ **Null reference crashes**: Fixed with proper timing and null checks
- ✅ **Smooth dragging**: Restored with interpolated movement
- ✅ **ZIndex ordering**: Fixed after drag operations
- ✅ **Centered scaling**: Fixed for hover and drag animations
- ✅ **CardSlot elimination**: Simplified architecture with direct Card → OrderedContainer
- ✅ **State-driven ordering**: Reorder operations update game state, visual follows
- ✅ **Position-driven reordering**: ReorderCardsCommand now properly updates CardState.Position values

## Architecture Summary
The card system now has a clean, simplified architecture:
- **Card**: Unified component implementing IOrderable, handles all card functionality
- **Hand**: Manages Cards directly in OrderedContainer, syncs with game state
- **OrderedContainer**: Handles positioning and layout for Cards
- **ReorderCardsCommand**: Updates card positions in game state
- **State-driven**: All card behavior driven by game state changes

No more intermediate CardSlot component - the architecture is now more direct and maintainable.
## 2025-08-12 — Hover state migrated to GameState

- Hover is now tracked in CardState (IsHovering) and managed via CardsState helpers.
- Added hover commands: StartHoverCommand and EndHoverCommand for pure state updates.
- Card input now dispatches hover commands; visuals react to state.
- Start drag clears hover to avoid inconsistent states.
- Removed HoverManager usage and registration; excluded deprecated files from build.

Files:
- State model: [Scripts/State/CardState.cs](Scripts/State/CardState.cs), [Scripts/State/CardsState.cs](Scripts/State/CardsState.cs)
- Commands: [Scripts/Commands/Card/HoverCardCommand.cs](Scripts/Commands/Card/HoverCardCommand.cs), [Scripts/Commands/Card/DragCardCommand.cs](Scripts/Commands/Card/DragCardCommand.cs)
- Card behavior: [Scripts/Implementations/Card/Card.cs](Scripts/Implementations/Card/Card.cs)
- Service registration cleanup: [Scripts/Implementations/Infra/ServiceLocator.cs](Scripts/Implementations/Infra/ServiceLocator.cs)
- Build excludes (safe to delete deprecated files): [Maximagus.csproj](Maximagus.csproj)