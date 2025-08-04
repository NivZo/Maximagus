using System;
using System.Collections.Generic;
using Godot;

public class CutsceneProvider : IDisposable
{
    private Timer _timer;
    private Action _nextAction;
    private Queue<CutsceneAction> _queue = new();

    // Take Action after Delay seconds
    public record CutsceneAction(Action Action, float Delay);

    public CutsceneProvider(Node parent)
    {
        _timer = new Timer() { Autostart = false, OneShot = true };
        _timer.Timeout += HandleTimeout;
        parent.AddChild(_timer);
    }

    public void Play(List<CutsceneAction> cutsceneActions)
    {
        var isEmpty = _queue.Count == 0;
        cutsceneActions.ForEach(_queue.Enqueue);

        if (isEmpty)
        {
            PlayInternal();
        }
    }

    public void Dispose()
    {
        _queue.Enqueue(new(() =>
        {
            _timer.QueueFree();
            _queue.Clear();
        }, 0));
    }

    private void PlayInternal()
    {
        var next = _queue.Dequeue();
        _nextAction = next.Action;
        if (next.Delay == 0)
        {
            HandleTimeout();
        }
        else
        {
            _timer.Start(next.Delay);
        }
    }

    private void HandleTimeout()
    {
        _nextAction();
        if (_queue.Count != 0)
        {
            PlayInternal();
        }
        else
        {
            _timer.Stop();
        }
    }
}