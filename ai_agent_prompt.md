# Spell System Implementation Prompt

## Context
Create a combo-based spell system for a Godot 4.4 C# card game similar to Balatro. Players submit ordered lists of cards that form spells, where cards can combo together while maintaining independence (non-blocking behavior).

## Core Requirements

### 1. Combo Philosophy
- Cards should always try to create the **longest valid combo possible**
- Cards that don't combo together should **never block or ruin each other**
- Each card should both **affect** and **consume from** the current spell context
- Example: `[Double Fire Modifier] → [Frost Spell] → [Fire Spell]` should still have the Fire Spell benefit from the modifier, as the Frost Spell didn't consume the fire modifier

### 2. Architecture Requirements
- **Plug-and-play design**: Should integrate with existing card drag/drop/submit system
- **Godot Resources**: Use Godot's resource system for all card definitions
- **Context-driven**: Shared spell context that persists throughout spell execution
- **Priority-based execution**: Cards execute by type priority (Modifiers → Actions → Utilities) then by individual priority

## Implementation Structure

### Core Classes to Create

#### 1. SpellCardResource (Abstract Base)
```csharp
[GlobalClass]
public abstract partial class SpellCardResource : Resource
{
    [Export] public string CardId { get; set; }
    [Export] public string CardName { get; set; }
    [Export] public CardType CardType { get; set; } // Action, Modifier, Utility
    [Export] public int ExecutionPriority { get; set; }
    
    public abstract void Execute(SpellContext context);
    public abstract bool CanInteractWith(SpellContext context);
    public virtual int GetExecutionPriority() => ExecutionPriority;
}
```

#### 2. SpellContext (Context Manager)
```csharp
public partial class SpellContext : RefCounted
{
    // Properties: Dictionary<string, Variant> for spell state
    // ActiveModifiers: Array of modifiers that can affect future cards
    // QueuedEffects: Array of effects to apply after spell completion
    // Target: The spell target (enemy, player, etc.)
    
    // Key Methods:
    // - GetProperty<T>(key, defaultValue)
    // - SetProperty<T>(key, value)
    // - ModifyProperty(key, modifier, ModifierType)
    // - AddModifier(modifier)
    // - ApplyDamageModifiers(baseDamage, damageType)
}
```

#### 3. SpellProcessor (Main Orchestrator)
```csharp
public partial class SpellProcessor : Node
{
    [Signal] public delegate void SpellExecutedEventHandler(SpellResult result);
    [Signal] public delegate void CardExecutedEventHandler(SpellCardResource card, SpellContext context);
    
    public SpellResult ProcessSpell(Array<SpellCardResource> cards, ISpellTarget target)
    {
        // 1. Create SpellContext
        // 2. Sort cards by execution priority
        // 3. Execute each card in order
        // 4. Apply all queued effects
        // 5. Return SpellResult
    }
}
```

#### 4. Supporting Resources
- **SpellModifierResource**: Base for modifiers that affect other cards
- **SpellEffectResource**: Base for effects applied after spell completion
- **ActionCardResource, ModifierCardResource, UtilityCardResource**: Specialized base classes

## Card Type Behaviors

### Action Cards
- Deal damage, apply status effects, interact with enemies
- Execute during main phase
- Can benefit from active modifiers
- Should track their effects in context (e.g., "FireDamageDealt", "FireInstances")

### Modifier Cards
- Create modifiers that affect future cards in the spell
- Execute first (negative priority)
- Should specify what they modify (damage types, effect types, etc.)
- Can be consumed on use or persistent for the whole spell

### Utility Cards
- Provide support effects, card draw, healing, etc.
- Execute last (positive priority)
- Often synergize with what happened earlier in the spell
- Can read context to scale effects

## Key Design Patterns

### 1. Context Properties Pattern
```csharp
// Cards write to context
context.ModifyProperty("FireDamageDealt", damage, ModifierType.Add);
context.SetProperty("FireInstances", fireInstances + 1);

// Other cards read from context
float totalFireDamage = context.GetProperty<float>("FireDamageDealt", 0f);
int fireInstances = context.GetProperty<int>("FireInstances", 0);
```

### 2. Modifier Pattern
```csharp
// Modifier cards add themselves to context
public override void Execute(SpellContext context)
{
    context.AddModifier(this.CreateModifier());
}

// Action cards apply all relevant modifiers
float finalDamage = context.ApplyDamageModifiers(baseDamage, DamageType.Fire);
```

### 3. Effect Queuing Pattern
```csharp
// Cards queue effects instead of applying immediately
context.QueueEffect(new StatusEffectResource { StatusEffectName = "Burn", Stacks = 2 });

// Processor applies all effects after spell completion
```

## Integration Points

### Input Integration
```csharp
// Your existing card submission handler
public void OnCardsSubmitted(List<YourCardData> selectedCards)
{
    var spellCards = ConvertToSpellCardResources(selectedCards);
    var result = spellProcessor.ProcessSpell(spellCards, currentEnemy);
    HandleSpellResult(result);
}
```

### Resource Creation
- Create concrete card classes inheriting from ActionCardResource, ModifierCardResource, UtilityCardResource
- Use `[GlobalClass]` attribute for editor integration
- Export properties for designer tweaking

## Example Card Implementations Needed

1. **Fire Bolt** (ActionCardResource): Deals fire damage, tracks fire instances
2. **Double Fire Modifier** (ModifierCardResource): Doubles next fire damage (consumed on use)
3. **Status Heal Utility** (UtilityCardResource): Heals based on enemy status effect count

## Success Criteria

- Cards can combo together regardless of play order
- Non-synergistic cards don't interfere with each other
- System integrates cleanly with existing card selection UI
- All card data uses Godot Resources for editor integration
- Modifiers persist until consumed or spell ends
- Context tracks all relevant spell state for card interactions

## Technical Notes

- Use Godot Collections (Array<T>, Dictionary<K,V>) for C# interop
- Inherit from RefCounted for proper memory management
- Use Variant type for flexible property storage
- Implement proper error handling for card execution failures
- Include signals for UI feedback and animation triggers