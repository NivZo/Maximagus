# Code Review Implementation Checklist

## 🎯 Implementation Strategy
Start with immediate/simple fixes and gradually advance to complex architectural changes.

---

## Phase 1: Quick Wins & Dead Code Removal (TODAY)

### 1.1 Dead Code Cleanup ⚡ IMMEDIATE
- [x] **Delete `Scripts/Events/CommandEvents.cs`** - Completely dead file
- [x] **Clean up ServiceLocator commented code** - Remove old registrations
- [x] **Remove unused imports** - Fixed HandManager.cs and StatusEffectManager.cs
- [ ] **Remove verbose debug prints** - Clean up excessive logging

### 1.2 Configuration Constants ⚡ SIMPLE
- [x] **Create `Scripts/Config/GameConfig.cs`** - Centralize magic numbers
- [x] **Replace magic numbers in CardLogic** - DRAG_THRESHOLD, SELECTION_VERTICAL_OFFSET
- [x] **Replace magic numbers in Hand** - CardsCurveMultiplier, CardsRotationMultiplier
- [x] **Replace magic numbers in TurnStartCommand** - DEFAULT_MAX_HAND_SIZE

### 1.3 Simple Performance Quick Fixes ⚡ SIMPLE
- [x] **Cache expensive calculations in Hand.AdjustFanEffect()** - Created HandLayoutCache utility
- [x] **Add early returns to CardLogic._Process()** - Skip when card null, not visible, command system not ready
- [ ] **Optimize string concatenations** - Use StringBuilder where needed

---

## Phase 2: Critical Performance Fixes (NEXT 1-2 DAYS)

### 2.1 State Synchronization Performance 🔴 CRITICAL
- [ ] **Add state change events to GameState** - Granular change notifications
- [ ] **Create CardController for event-driven updates** - Replace polling
- [ ] **Implement indexed card lookups** - O(1) instead of O(n)
- [ ] **Remove frame-by-frame polling from CardLogic._Process()**

### 2.2 Hand Layout Optimization 🔴 HIGH IMPACT
- [ ] **Create HandLayoutCache utility** - Cache position calculations
- [ ] **Implement incremental fan updates** - Only update changed positions
- [ ] **Optimize AdjustFanEffect algorithm** - Reduce recalculations

---

## Phase 3: Architecture Improvements (NEXT 2-3 DAYS)

### 3.1 SOLID Principle Fixes 🟡 ARCHITECTURE
- [ ] **Split CardLogic responsibilities** - Create CardController, CardInputHandler
- [ ] **Extract CardVisualSynchronizer** - Handle visual state sync separately
- [ ] **Create CardPositionManager** - Handle positioning logic
- [ ] **Refactor Hand class** - Separate view from logic

### 3.2 Input System Consolidation 🟡 ARCHITECTURE
- [ ] **Remove input handling from CardLogic.OnGuiInput()** - Centralize inputs
- [ ] **Simplify feature flag system** - Binary on/off instead of complex switching
- [ ] **Create unified input pipeline** - Single flow for all inputs

---

## Phase 4: Code Quality Improvements (ONGOING)

### 4.1 Type Safety & Error Handling 🟢 QUALITY
- [ ] **Implement strongly-typed CardId struct** - Replace string IDs
- [ ] **Create Result<T> pattern** - Standardize error handling
- [ ] **Add parameter validation** - Consistent null checks

### 4.2 Dependency Injection 🟢 ARCHITECTURE
- [ ] **Create ServiceContainer** - Replace ServiceLocator anti-pattern
- [ ] **Implement constructor injection** - Remove hidden dependencies
- [ ] **Update component initialization** - Use DI throughout

---

## Progress Tracking

### ✅ Completed Tasks
*Tasks will be moved here as completed*

### 🚧 In Progress
*Current active task*

### ❌ Blocked/Issues
*Track any blockers encountered*

---

## Success Metrics

### Performance Targets
- [ ] **Frame rate**: Stable 60 FPS with 10 cards
- [ ] **State sync**: 90% CPU reduction in CardLogic._Process()
- [ ] **Memory**: 75% reduction in input processing allocations

### Code Quality Targets
- [ ] **SOLID compliance**: Zero violations in core components
- [ ] **Dead code**: All legacy artifacts removed
- [ ] **Magic numbers**: All constants centralized

### Architecture Targets
- [ ] **Single responsibility**: Each class has one clear purpose
- [ ] **Event-driven**: No polling for state changes
- [ ] **Testability**: Clear dependencies, no service locator

---

## Implementation Notes

### Risk Mitigation
- **Feature flags**: Keep for critical changes
- **Git branches**: One per major change
- **Incremental**: Test after each change

### Testing Strategy
- **Visual verification**: Ensure gameplay unchanged
- **Performance monitoring**: Measure before/after
- **Error checking**: No new exceptions introduced

---

## Next Session Priorities

1. **START HERE**: Dead code cleanup (30 minutes)
2. **THEN**: Configuration constants (30 minutes)  
3. **THEN**: Simple performance fixes (1 hour)
4. **ASSESS**: Ready for critical performance fixes

---

*This checklist will be updated throughout implementation to track progress and add new items as discovered.*