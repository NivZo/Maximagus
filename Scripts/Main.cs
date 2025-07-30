using Godot;
using System;
using System.Linq;
using Maximagus.Scripts.Spells.Implementations;
using Maximagus.Scripts.Spells.Resources;

public partial class Main : Control
{
    private ILogger _logger;
    private SpellProcessor _spellProcessor;

    public override void _EnterTree()
    {
        base._EnterTree();
        ServiceLocator.Initialize();
    }

    public override void _Ready()
    {
        try
        {
            base._Ready();
            _logger = ServiceLocator.GetService<ILogger>();
            _spellProcessor = new SpellProcessor();
            AddChild(_spellProcessor);

            _logger?.LogInfo("Main scene initialized successfully");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Critical error initializing Main: {ex}");
            throw;
        }
    }

    public override void _Input(InputEvent @event)
    {
        try
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                HandleKeyInput(keyEvent);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling input in Main", ex);
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        switch (keyEvent.Keycode)
        {
            case Key.Enter:
                var cards = Hand.Instance.SelectedCards;
                if (cards.Length == 0) return;

                GD.Print($"--- Submitting Spell ---\nSelected cards in hand order: {string.Join(", ", cards.Select(c => c.Resource.SpellCard?.CardName ?? "Unknown"))}");

                var spellCards = new Godot.Collections.Array<SpellCardResource>();
                foreach (var card in cards)
                {
                    if (card.Resource.SpellCard != null)
                    {
                        spellCards.Add(card.Resource.SpellCard);
                    }
                }

                if (spellCards.Count > 0)
                {
                    _spellProcessor.ProcessSpell(spellCards, null);
                }

                Hand.Instance.Discard(cards.ToArray());
                Hand.Instance.DrawAndAppend(cards.Length);
                break;
        }
    }
}