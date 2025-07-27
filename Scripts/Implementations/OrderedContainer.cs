using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OrderedContainer : ColorRect, IEnumerable<IOrderable>
{
    [Export] public bool Debug = false;

    public event Action ElementsChanged;

    private ILogger _logger;
    private readonly List<IOrderable> _elements = new();

    public override void _Ready()
    {
        try
        {
            _logger = ServiceLocator.GetService<ILogger>();

            Color = Debug ? Colors.Red : new Color(0, 0, 0, 0);
            CallDeferred(MethodName.RecalculatePositions);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing OrderedContainer", ex);
            throw;
        }
    }

    public override void _EnterTree()
    {
        Resized += () => CallDeferred(MethodName.RecalculatePositions);
    }

    public override void _ExitTree()
    {
        Resized -= () => CallDeferred(MethodName.RecalculatePositions);
    }

    public IOrderable this[int index]
    {
        get
        {
            index.ValidateIndex(_elements.Count, nameof(index));
            return _elements[index];
        }
        set
        {
            try
            {
                value.ValidateNotNull(nameof(value));
                
                // Expand list if necessary
                while (index >= _elements.Count)
                {
                    _elements.Add(null);
                }

                _elements[index] = value;
                CallDeferred(MethodName.RecalculatePositions);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error setting element at index {index}", ex);
                throw;
            }
        }
    }
    
    public IEnumerator<IOrderable> GetEnumerator()
    {
        return _elements.Where(e => e != null).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _elements.Count;

    public void InsertElement(int index, IOrderable element)
    {
        try
        {
            element.ValidateNotNull(nameof(element));
            
            if (index < 0) index = 0;
            if (index > _elements.Count) index = _elements.Count;
            
            _elements.Insert(index, element);
            CallDeferred(MethodName.RecalculatePositions);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error inserting element at index {index}", ex);
            throw;
        }
    }

    public IOrderable RemoveElement(IOrderable element)
    {
        try
        {
            if (element == null) return null;

            int index = _elements.IndexOf(element);
            return index >= 0 ? RemoveElementAt(index) : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error removing element", ex);
            throw;
        }
    }

    public IOrderable RemoveElementAt(int index)
    {
        try
        {
            if (index < 0 || index >= _elements.Count)
                return null;

            var element = _elements[index];
            _elements.RemoveAt(index);
            CallDeferred(MethodName.RecalculatePositions);
            
            return element;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error removing element at index {index}", ex);
            throw;
        }
    }

    public void MoveElement(int fromIndex, int toIndex)
    {
        try
        {
            if (!IsValidMoveOperation(fromIndex, toIndex))
                return;

            // Move element in-place by swapping adjacent elements
            if (fromIndex < toIndex)
            {
                // Moving forward: swap with each element to the right
                for (int i = fromIndex; i < toIndex; i++)
                {
                    (_elements[i], _elements[i + 1]) = (_elements[i + 1], _elements[i]);
                }
            }
            else
            {
                // Moving backward: swap with each element to the left
                for (int i = fromIndex; i > toIndex; i--)
                {
                    (_elements[i], _elements[i - 1]) = (_elements[i - 1], _elements[i]);
                }
            }

            CallDeferred(MethodName.RecalculatePositions);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error moving element from {fromIndex} to {toIndex}", ex);
            throw;
        }
    }

    public void SwapElements(int indexA, int indexB)
    {
        try
        {
            if (!IsValidSwapOperation(indexA, indexB))
                return;

            (_elements[indexA], _elements[indexB]) = (_elements[indexB], _elements[indexA]);
            CallDeferred(MethodName.RecalculatePositions);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error swapping elements at indices {indexA} and {indexB}", ex);
            throw;
        }
    }

    private bool IsValidMoveOperation(int fromIndex, int toIndex)
    {
        return fromIndex >= 0 && fromIndex < _elements.Count &&
               toIndex >= 0 && toIndex < _elements.Count &&
               fromIndex != toIndex;
    }

    private bool IsValidSwapOperation(int indexA, int indexB)
    {
        return indexA >= 0 && indexA < _elements.Count &&
               indexB >= 0 && indexB < _elements.Count &&
               indexA != indexB;
    }

    public void RecalculatePositions()
    {
        try
        {
            var validElements = _elements.Where(e => e != null).ToList();
            if (validElements.Count == 0)
                return;

            bool isHorizontal = Size.X > Size.Y;

            if (isHorizontal)
            {
                CalculateHorizontalPositions(validElements);
            }
            else
            {
                CalculateVerticalPositions(validElements);
            }

            ElementsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error recalculating positions", ex);
        }
    }

    private void CalculateHorizontalPositions(List<IOrderable> elements)
    {
        float totalHorizontalWeight = elements.Sum(element => element.Weight.X);
        float availableWidth = Size.X;
        float currentX = 0;

        foreach (var element in elements)
        {
            float elementWidth = (availableWidth * element.Weight.X) / totalHorizontalWeight;
            float xPos = currentX + (elementWidth / 2);
            float yPos = Size.Y / 2;

            element.TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
            currentX += elementWidth;
        }
    }

    private void CalculateVerticalPositions(List<IOrderable> elements)
    {
        float totalVerticalWeight = elements.Sum(element => element.Weight.Y);
        float availableHeight = Size.Y;
        float currentY = 0;

        foreach (var element in elements)
        {
            float elementHeight = (availableHeight * element.Weight.Y) / totalVerticalWeight;
            float xPos = Size.X / 2;
            float yPos = currentY + (elementHeight / 2);

            element.TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
            currentY += elementHeight;
        }
    }
}