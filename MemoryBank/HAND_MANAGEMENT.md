# Hand Management

The hand management system governs how players draw, play, and discard cards.

- **`HandManager`**: A service that manages the player's hand. It is responsible for drawing new cards, submitting hands for play or discard, and enforcing rules such as hand size and the number of plays/discards per turn.
- **Hand Actions:** The system distinguishes between two primary hand actions: `Play` and `Discard`. These are tracked separately, allowing for different limits and mechanics for each.
- **Events:** The `HandManager` communicates with other systems via the `IEventBus`, publishing events such as `HandSubmittedEvent` and `CardsRedrawEvent`.
