using Godot;
using System;
using System.Collections.Generic;

public partial class QueuedActionsManager : Node
{
    public struct QueuedAction
    {
        public Action Action { get; set; }
        public float DelayBefore { get; set; }
        public float DelayAfter { get; set; }
        
        public QueuedAction(Action action, float delayBefore = 0f, float delayAfter = 0f)
        {
            Action = action;
            DelayBefore = delayBefore;
            DelayAfter = delayAfter;
        }
    }
    
    private Queue<QueuedAction> _actionQueue = new Queue<QueuedAction>();
    private bool _isProcessingActions = false;
    private float _currentDelayTimer = 0f;
    private bool _isWaitingBeforeAction = false;
    private QueuedAction? _currentAction = null;
    
    public bool IsProcessingActions => _isProcessingActions;
    public int QueueCount => _actionQueue.Count;
    
    public override void _Ready()
    {
        SetProcess(true);
    }
    
    public override void _Process(double delta)
    {
        if (!_isProcessingActions && _actionQueue.Count > 0)
        {
            StartProcessingNext();
        }
        
        if (_isProcessingActions)
        {
            ProcessCurrentAction((float)delta);
        }
    }
    
    public void QueueAction(Action action, float delayBefore = 0f, float delayAfter = 0f)
    {
        if (action == null)
        {
            GD.PrintErr("ActionQueue: Attempted to queue a null action");
            return;
        }
        
        var queuedAction = new QueuedAction(action, delayBefore, delayAfter);
        _actionQueue.Enqueue(queuedAction);
    }
    
    public void QueueActions(params QueuedAction[] actions)
    {
        foreach (var action in actions)
        {
            if (action.Action != null)
            {
                _actionQueue.Enqueue(action);
            }
            else
            {
                GD.PrintErr("ActionQueue: Attempted to queue a null action in batch");
            }
        }
    }
    
    public void ClearQueue()
    {
        _actionQueue.Clear();
    }
    
    public void StopAndClear()
    {
        _isProcessingActions = false;
        _isWaitingBeforeAction = false;
        _currentDelayTimer = 0f;
        _currentAction = null;
        _actionQueue.Clear();
    }
    
    private void StartProcessingNext()
    {
        if (_actionQueue.Count == 0) return;
        
        _currentAction = _actionQueue.Dequeue();
        _isProcessingActions = true;
        
        if (_currentAction.Value.DelayBefore > 0f)
        {
            _isWaitingBeforeAction = true;
            _currentDelayTimer = _currentAction.Value.DelayBefore;
        }
        else
        {
            ExecuteCurrentAction();
        }
    }
    
    private void ProcessCurrentAction(float delta)
    {
        if (!_currentAction.HasValue) return;
        
        if (_isWaitingBeforeAction)
        {
            _currentDelayTimer -= delta;
            if (_currentDelayTimer <= 0f)
            {
                _isWaitingBeforeAction = false;
                ExecuteCurrentAction();
            }
            return;
        }
        
        if (_currentDelayTimer > 0f)
        {
            _currentDelayTimer -= delta;
            if (_currentDelayTimer <= 0f)
            {
                FinishCurrentAction();
            }
        }
    }
    
    private void ExecuteCurrentAction()
    {
        if (!_currentAction.HasValue) return;
        
        try
        {
            _currentAction.Value.Action.Invoke();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ActionQueue: Error executing action - {ex.Message}");
        }
        
        if (_currentAction.Value.DelayAfter > 0f)
        {
            _currentDelayTimer = _currentAction.Value.DelayAfter;
        }
        else
        {
            FinishCurrentAction();
        }
    }
    
    private void FinishCurrentAction()
    {
        _currentAction = null;
        _isProcessingActions = false;
        _currentDelayTimer = 0f;
    }
    
    public void QueueActionWithDelay(Action action, float delay)
    {
        QueueAction(action, delay, 0f);
    }
    
    public void QueueActionWithPause(Action action, float pauseAfter)
    {
        QueueAction(action, 0f, pauseAfter);
    }
    
    public void QueueDelay(float delay)
    {
        QueueAction(() => { }, delay, 0f);
    }
}