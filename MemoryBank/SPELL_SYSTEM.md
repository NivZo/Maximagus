# Spell System

The spell system is the core of the gameplay. It uses a command-driven architecture where spell processing is managed through the centralized GameState and GameCommandProcessor.

- **`SpellCardResource`**: An abstract base class for all spell cards. It defines the basic properties of a card, such as its type (Action, Modifier, Utility) and execution priority. Cards create execution commands rather than executing directly.
- **`SpellState`**: A centralized state object that tracks active spell progress, including modifiers, properties, damage dealt, and spell history. This replaces the old SpellContext system.
- **`SpellLogicManager`**: A static manager class containing pure functions for spell calculations and state updates. Handles damage calculations, modifier application, and property updates.
- **`GameCommandProcessor`**: The main orchestrator of the spell system. Processes spell commands sequentially, maintaining state consistency and enabling proper error handling and rollback capabilities.
- **Spell Commands**: Individual commands like `StartSpellCommand`, `ExecuteCardActionCommand`, and `CompleteSpellCommand` that handle specific aspects of spell processing through the command system.
