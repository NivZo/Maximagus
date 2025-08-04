# Recommended Game State Flow

## 1. Input Layer
```
User Input → InputToActionMapper → GameCommand
```
- Mouse clicks, keyboard input
- Convert to semantic game commands
- Validate input context

## 2. Command Processing Layer
```
GameCommand → GameCommandProcessor → GameState Updates
```
- Single point for all state changes
- Command validation against current state
- History tracking for undo/redo

## 3. State Management Layer
```
GameState (Single Source of Truth)
├── GamePhase (Menu, CardSelection, SpellCasting, etc.)
├── HandState (Cards, selections, positions)
├── PlayerState (health, mana, etc.)
└── EncounterState (enemy, turn counter, etc.)
```

## 4. Event Propagation Layer
```
State Change → EventBus → Relevant Systems
```
- GameLogicSystem (derived state updates)
- VisualControllers (UI updates)
- AudioSystem (sound effects)
- AnalyticsSystem (tracking)

## 5. View Layer
```
StateEvents → VisualControllers → Godot Nodes
```
- Each visual component has a controller
- Controllers subscribe to relevant state events
- Pure visual updates (no game logic)

## Key Benefits

### 🎯 **Single Source of Truth**
- All game state in one place
- No synchronization issues
- Easy to debug and inspect

### 🔄 **Predictable Flow**
- Input → Command → State → Event → View
- Easy to trace any interaction
- Clear separation of concerns

### 🧪 **Testable**
- Game logic separated from Godot nodes
- Commands can be unit tested
- State transitions are deterministic

### 🔧 **Maintainable**
- New features just add new commands
- Visual changes don't affect game logic
- Clear boundaries between systems

## Example Flow: Player Clicks Card

```
1. Card receives mouse click
2. CardInputHandler calls InputToActionMapper.HandleCardClick(cardId)
3. InputToActionMapper creates SelectCardCommand(cardId)
4. GameCommandProcessor validates and executes command
5. Command updates GameState.Hand.Cards[cardId].IsSelected
6. GameState publishes CardSelectionChangedEvent
7. CardVisualController receives event and updates visuals
8. GameLogicSystem receives event and validates selection rules
```

## Implementation Priority

1. **Phase 1**: Implement GameState and GameCommandProcessor
2. **Phase 2**: Refactor input handling to use commands
3. **Phase 3**: Move card state out of Card nodes into GameState
4. **Phase 4**: Implement visual controllers that respond to state events
5. **Phase 5**: Clean up existing event system to match new architecture