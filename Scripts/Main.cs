using Godot;
using System;
using System.Linq;

public partial class Main : Control
{
    private ILogger _logger;

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
                GD.Print($"Selected cards with values: {string.Join(", ", cards.Select(c => c.Resource.Value))}");
                Hand.Instance.Discard(cards.ToArray());
                Hand.Instance.DrawAndAppend(cards.Length);
                break;
        }
    }
}