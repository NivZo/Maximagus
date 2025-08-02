using System;
using System.Collections.Generic;
using Godot;
using Maximagus.Scripts.Spells.Abstractions;

public class Deck
{
    private List<SpellCardResource> _availableSpells;

    public Deck()
    {
        _availableSpells =
        [
            ResourceLoader.Load<SpellCardResource>("res://Resources/Spells/FrostShards.tres"),
            ResourceLoader.Load<SpellCardResource>("res://Resources/Spells/Firebolt.tres"),
            ResourceLoader.Load<SpellCardResource>("res://Resources/Spells/AmplifyFire.tres"),
            ResourceLoader.Load<SpellCardResource>("res://Resources/Spells/AmplifyFrost.tres"),
        ];
    }

    public SpellCardResource GetNext()
    {
        var rnd = new Random();
        return _availableSpells[rnd.Next(_availableSpells.Count)];
    }
}