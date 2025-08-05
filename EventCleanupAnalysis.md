# Event System Cleanup Analysis

## 📊 **CURRENT EVENT USAGE ANALYSIS**

### **Events Being PUBLISHED:**

#### **1. Command System Events (✅ KEEP - New Architecture)**
- `GameStateChangedEventData` - Published by GameCommandProcessor 
  - **Usage**: Core command system event for state changes
  - **Status**: ✅ KEEP - Essential part of new command architecture

#### **2. Visual/UI Events (✅ KEEP - Visual Effects Only)**
- `CardDragStartedEvent` - Published by CardLogic.cs:262  
- `CardDragEndedEvent` - Published by CardLogic.cs:275
- `CardHoverStartedEvent` - Published by CardLogic.cs:303
- `CardHoverEndedEvent` - Published by CardLogic.cs:312  
- `CardMouseMovedEvent` - Published by CardLogic.cs:320
- `CardPositionChangedEvent` - Published by CardLogic.cs:362
- `HandCardSlotsChangedEvent` - Published by Hand.cs:119
  - **Usage**: Visual effects, animations, UI feedback
  - **Status**: ✅ KEEP - Pure visual events, not game state related

#### **3. Legacy Command Events (❌ REMOVE - Replaced by Command System)**
- `StartGameRequestedEvent` - Published by GameInputManager.cs:38
- `PlayCardsRequestedEvent` - Published by GameInputManager.cs:43  
- `DiscardCardsRequestedEvent` - Published by GameInputManager.cs:48
- `CastSpellRequestedEvent` - Published by HandManager.cs:42
  - **Usage**: Legacy command pattern, replaced by pure command system
  - **Status**: ❌ REMOVE - No longer needed

### **Events Being SUBSCRIBED To:**

#### **1. Visual Effects (✅ KEEP)**
- `CardDragStartedEvent` - CardVisual.cs:112
- `CardDragEndedEvent` - CardVisual.cs:113  
- `CardHoverStartedEvent` - CardVisual.cs:114, Hand.cs:76
- `CardHoverEndedEvent` - CardVisual.cs:115, Hand.cs:77
- `CardPositionChangedEvent` - CardVisual.cs:117
- `CardMouseMovedEvent` - CardVisual.cs:118
- `HandCardSlotsChangedEvent` - CardLogic.cs:82
  - **Status**: ✅ KEEP - Pure visual events

#### **2. Legacy Events (❌ REMOVE - Replaced by Command System)**  
- `CardClickedEvent` - CardVisual.cs:116 (SUBSCRIBED BUT NEVER PUBLISHED!)
- `PlayCardsRequestedEvent` - HandManager.cs:30
- `DiscardCardsRequestedEvent` - HandManager.cs:31
- `CastSpellRequestedEvent` - SpellProcessingManager.cs:25
  - **Status**: ❌ REMOVE - Pure command system handles this

## 🚨 **CRITICAL FINDING: UNUSED EVENT**

### **CardClickedEvent - NEVER PUBLISHED!**
- **Subscribed**: CardVisual.cs:116
- **Published**: NOWHERE! (Removed when we replaced with command system)
- **Handler**: `OnCardClicked` method in CardVisual.cs
- **Status**: ❌ DEAD CODE - Remove entirely

## 🧹 **CLEANUP PLAN**

### **Phase 1: Remove Legacy Command Events**
**Files to Clean:**
1. **Scripts/Events/CommandEvents.cs**
   - Remove: `StartGameRequestedEvent`, `PlayCardsRequestedEvent`, `DiscardCardsRequestedEvent`, `CastSpellRequestedEvent`

2. **Scripts/Implementations/Managers/GameInputManager.cs**
   - Remove: All event publishing (lines 38, 43, 48)
   - Replace with direct command execution

3. **Scripts/Implementations/Managers/HandManager.cs**  
   - Remove: Event subscriptions (lines 30, 31)
   - Remove: Event publishing (line 42)
   - Remove: Event handlers `HandlePlayCardsRequested`, `HandleDiscardCardsRequested`

4. **Scripts/Implementations/Spell/SpellProcessingManager.cs**
   - Remove: Event subscription (line 25)
   - Remove: Event handler `HandleCastSpellRequest`

### **Phase 2: Remove Dead CardClickedEvent**
**Files to Clean:**
1. **Scripts/Events/CardEvents.cs**
   - Remove: `CardClickedEvent` class entirely

2. **Scripts/Implementations/Card/CardVisual.cs**
   - Remove: `CardClickedEvent` subscription (line 116)
   - Remove: `CardClickedEvent` unsubscription (line 127) 
   - Remove: `OnCardClicked` method entirely

### **Phase 3: Simplify GameInputManager**
**After removing legacy events, GameInputManager should:**
- Execute commands directly instead of publishing events
- Remove event bus dependency entirely
- Become a pure input-to-command mapper

## ✅ **EVENTS TO KEEP (Visual Effects Only)**

### **Card Visual Events (All Working)**
- `CardDragStartedEvent/CardDragEndedEvent` - Drag visual effects
- `CardHoverStartedEvent/CardHoverEndedEvent` - Hover visual effects  
- `CardMouseMovedEvent` - Mouse tracking for tooltips
- `CardPositionChangedEvent` - Animation and positioning
- `HandCardSlotsChangedEvent` - Hand layout updates

### **Command System Events (Core Architecture)**
- `GameStateChangedEventData` - State change notifications

## 📈 **EXPECTED BENEFITS**

### **Code Reduction:**
- Remove ~100 lines of dead event code
- Remove 4 unused event classes
- Remove 6 event handlers  
- Remove event bus dependencies in GameInputManager

### **Architecture Cleanup:**
- Pure command system without legacy event mixing
- Cleaner separation: Visual events vs Command system
- No more dead code subscriptions
- Reduced complexity and maintenance burden

### **Performance Improvement:**
- No more unused event subscriptions
- Reduced memory overhead from dead handlers
- Cleaner event bus with only active subscriptions

## 🎯 **IMPLEMENTATION PRIORITY**

1. **HIGH**: Remove `CardClickedEvent` (dead code causing confusion)
2. **HIGH**: Remove legacy command events (architectural cleanup)  
3. **MEDIUM**: Simplify GameInputManager (direct command execution)
4. **LOW**: Documentation update (reflect new event architecture)

This cleanup will result in a much cleaner event system with only essential visual events and the core command system events.