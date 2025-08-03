# Project Overview

This project is a combo-based spell-casting card game, developed in Godot 4.4 with C#. The core gameplay is inspired by titles like *Balatro*, where players construct spells by combining cards in a specific order. The system is designed to be highly flexible, allowing for complex card interactions and emergent strategies.

## Core Mechanics

- **Spell-casting:** Players submit an ordered list of cards to form a spell.
- **Combo System:** Cards can combo with each other to create powerful effects. The system is designed to find the longest possible valid combo from the submitted cards.
- **Non-blocking Card Interactions:** Cards that do not synergize with each other do not prevent other cards from being part of a combo.
- **Resource-based Definitions:** All cards, spells, and status effects are defined as Godot Resources, allowing for easy creation and modification in the editor.
