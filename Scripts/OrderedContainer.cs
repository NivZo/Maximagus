using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OrderedContainer : ColorRect, IEnumerable<IOrderable>
{
    [Export] public bool Debug = false;

    public event Action ElementsChanged;

    protected IOrderable[] _elements;

    public override void _Ready()
    {
        Color = Debug ? Color : new Color(0, 0, 0, 0);
        RecalculatePositions();
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
        get => _elements[index];
        set
        {
            if (_elements is null)
            {
                _elements = new IOrderable[1];
                _elements[0] = value;
            }
            else if (index >= _elements.Length)
            {
                var newElements = new IOrderable[index + 1];
                for (int i = 0; i < _elements.Length; i++)
                {
                    newElements[i] = _elements[i];
                }
                newElements[index] = value;
                _elements = newElements;
            }

            RecalculatePositions();
        }
    }
    
    public IEnumerator<IOrderable> GetEnumerator()
    {
        return ((IEnumerable<IOrderable>)_elements).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _elements.GetEnumerator();
    }

    public int Count => _elements.Length;

    public void InsertElement(int index, IOrderable element)
    {
        _elements.Append(element);
        MoveElement(_elements.Length - 1, index);
    }

    public IOrderable RemoveElement(IOrderable element)
    {
        if (_elements == null || _elements.Length == 0)
            return null;

        int index = Array.IndexOf(_elements, element);
        return RemoveElementAt(index);
    }

    public IOrderable RemoveElementAt(int index)
    {
        if (_elements == null || _elements.Length == 0 || index < 0 || index >= _elements.Length)
            return null;

        var newElements = new List<IOrderable>(_elements);
        var element = newElements[index];
        newElements.RemoveAt(index);
        _elements = newElements.ToArray();
        RecalculatePositions();

        return element;
    }

    public void MoveElement(int fromIndex, int toIndex)
    {
        if (_elements == null || _elements.Length == 0)
            return;

        if (fromIndex < 0 || fromIndex >= _elements.Length ||
            toIndex < 0 || toIndex >= _elements.Length ||
            fromIndex == toIndex)
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

        RecalculatePositions();
    }

    public void SwapElements(int indexA, int indexB)
    {
        if (_elements == null || _elements.Length == 0 ||
            indexA < 0 || indexA >= _elements.Length ||
            indexB < 0 || indexB >= _elements.Length ||
            indexA == indexB)
            return;

        (_elements[indexA], _elements[indexB]) = (_elements[indexB], _elements[indexA]);
        RecalculatePositions();
    }

    public void RecalculatePositions()
    {
        if (_elements == null || _elements.Length == 0)
        {
            return;
        }

        int elementCount = _elements.Length;
        bool isHorizontal = Size.X > Size.Y;

        if (isHorizontal)
        {
            // Calculate total horizontal weight
            float totalHorizontalWeight = _elements.Sum(element => element.Weight.X);
            
            float availableWidth = Size.X;
            float currentX = 0;

            for (int i = 0; i < elementCount; i++)
            {
                // Calculate the width this element should occupy based on its horizontal weight
                float elementWidth = (availableWidth * _elements[i].Weight.X) / totalHorizontalWeight;
                
                // Position at the center of this element's allocated space
                float xPos = currentX + (elementWidth / 2);
                float yPos = Size.Y / 2;

                _elements[i].TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
                
                // Move to the next position
                currentX += elementWidth;
            }
        }
        else
        {
            // Calculate total vertical weight
            float totalVerticalWeight = _elements.Sum(element => element.Weight.Y);
            
            float availableHeight = Size.Y;
            float currentY = 0;

            for (int i = 0; i < elementCount; i++)
            {
                // Calculate the height this element should occupy based on its vertical weight
                float elementHeight = (availableHeight * _elements[i].Weight.Y) / totalVerticalWeight;
                
                // Position at the center of this element's allocated space
                float xPos = Size.X / 2;
                float yPos = currentY + (elementHeight / 2);

                _elements[i].TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
                
                // Move to the next position
                currentY += elementHeight;
            }
        }

        ElementsChanged?.Invoke();
    }
}
