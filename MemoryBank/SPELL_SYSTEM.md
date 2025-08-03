# Spell System

The spell system is the core of the gameplay. It is orchestrated by the `SpellProcessor` and relies on a `SpellContext` to manage the state of a spell as it is being cast.

- **`SpellCardResource`**: An abstract base class for all spell cards. It defines the basic properties of a card, such as its type (Action, Modifier, Utility) and execution priority. Concrete card implementations inherit from this class.
- **`SpellContext`**: A context object that is created for each spell. It holds the state of the spell as it resolves, including active modifiers, queued effects, and other relevant data. Cards can read from and write to the context, allowing for complex interactions.
- **`SpellProcessor`**: The main orchestrator of the spell system. It takes a list of `SpellCardResource` objects, sorts them by priority, and executes them in order. It also manages the lifecycle of the `SpellContext`.
