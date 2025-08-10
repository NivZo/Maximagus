# Hand Management

## Card Drawing - State-Driven Architecture

The process of adding cards to the hand follows a state-driven architecture pattern:

1. **TurnStartCommand** calls `handManager.DrawCard()` to get a card resource ID from the deck
2. **TurnStartCommand** creates and executes an `AddCardCommand` with the card resource ID
3. **AddCardCommand** adds the card to the game state
4. State changes trigger the **Hand** component's `OnHandStateChanged` observer method
5. **Hand** creates visual card nodes based on the updated state and adds them to the scene

This replaces the previous reverse-state-driven implementation where:
- HandManager.Hand.DrawAndAppend would add nodes to the scene first
- Then send AddCardCommand for execution in the processor
- AddCardCommand would add the card to the state afterward

The new state-driven architecture ensures that:
1. The game state is the single source of truth
2. UI components (Hand) react to state changes rather than driving them
3. Commands modify state directly, and UI observes these changes
4. The data flow is unidirectional: Commands → State → UI

## Legacy Code

The following methods are maintained for backward compatibility but marked as legacy:
- `Hand.DrawAndAppend(int amount)` - Has been modified to use the state-driven approach internally

## Components

### HandManager
- Provides `DrawCard()` method that returns a card resource ID from the deck
- No longer directly modifies the UI

### Hand
- Observes state changes via `_commandProcessor.StateChanged` event
- Creates visual cards when state changes indicate cards were added
- `SyncVisualCardsWithState` ensures UI matches the current state
- `CreateVisualCardFromState` handles creating visual cards from state

### AddCardCommand
- Creates and adds a CardState to the game state
- No direct interaction with UI components

### TurnStartCommand
- Orchestrates the card drawing process by:
  - Getting card resource IDs via HandManager.DrawCard()
  - Executing AddCardCommand to update state
  - Letting state changes trigger UI updates

## Cards State Centralization (2025-08-10)

We refactored card placement state out of HandState into a single, immutable CardsState that tracks all cards across containers: Hand, PlayedCards, DiscardedCards.

Key principles:
- Single source of truth: CardsState holds all CardState instances and their ContainerType.
- HandState now only contains settings (MaxHandSize, IsLocked).
- UI reacts to GameState changes; commands mutate state only.

Core APIs (CardsState):
- Queries: HandCards, PlayedCards, DiscardedCards, SelectedInHand, InHandCount, DraggingInHand.
- Mutations: 
  - WithAddedCard(CardState)
  - WithRemovedCard(string) / WithRemovedCards(IEnumerable&lt;string&gt;)
  - WithCardSelection(string, bool)
  - WithCardDragging(string, bool)
  - WithClearedSelectionInHand()
  - WithMovedToContainer(IEnumerable&lt;string&gt; ids, ContainerType target)
  - WithReorderedHandCards(IReadOnlyList&lt;string&gt; newOrder)

Updated flows:
- Selection: Select/Deselect toggle selection via CardsState; eligibility checks confirm card is in Hand.
- Drag: StartDrag/EndDrag set IsDragging on the specific card; UI reads Cards.DraggingInHand.
- Reorder: ReorderCardsCommand uses WithReorderedHandCards.
- Play: SelectedInHand moved to PlayedCards and cleared selection; phase -> SpellCasting.
- SpellCast visualization reads Cards.PlayedCards for execution.
- TurnEnd: PlayedCards moved to Discarded; phase progresses.

Touched code (high level):
- Spell processing uses centralized played cards.
- Input mapping reads selection from CardsState.
- CardContainer drag logic reads dragging state from CardsState.
- Commands (Select/Deselect/Drag/Play/TurnEnd/Reorder/Add) mutate CardsState and rebuild GameState via WithCards(...).

Rationale:
- Improves clarity: placement is not tied to HandState.
- Easier to extend with more containers.
- Maintains unidirectional data flow: Commands → State → UI.

Build status: dotnet build succeeded post-refactor.
