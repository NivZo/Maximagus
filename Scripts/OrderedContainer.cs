using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

public partial class OrderedContainer : ColorRect, IEnumerable<IOrderable>
{
    [Export] public bool Debug = false;

    [Export(PropertyHint.Range, "0,100,1")]
    public float Padding = 10.0f;

    protected IOrderable[] _elements;

    public override void _Ready()
    {
        Color = Debug ? Color : new Color(0, 0, 0, 0);
        RecalculatePositions();
    }

    public override void _EnterTree()
    {
        Resized += RecalculatePositions;
        GetWindow().SizeChanged += RecalculatePositions;
    }

    public override void _ExitTree()
    {
        Resized -= RecalculatePositions;
        GetWindow().SizeChanged -= RecalculatePositions;
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

    protected void RecalculatePositions()
    {
        if (_elements == null || _elements.Length == 0)
        {
            return;
        }

        int elementCount = _elements.Length;
        bool isHorizontal = Size.X > Size.Y;

        if (isHorizontal)
        {
            float availableWidth = Size.X - (Padding * 2);
            float segmentWidth = availableWidth / (elementCount + 1);

            for (int i = 0; i < elementCount; i++)
            {
                float xPos = Padding + segmentWidth * (i + 1);
                float yPos = Size.Y / 2;

                _elements[i].TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
                _elements[i].TargetSize = new Vector2(segmentWidth, GetWindow().Size.Y * 2);
            }
        }
        else
        {
            float availableHeight = Size.Y - (Padding * 2);
            float segmentHeight = availableHeight / (elementCount + 1);

            for (int i = 0; i < elementCount; i++)
            {
                float xPos = Size.X / 2;
                float yPos = Padding + segmentHeight * (i + 1);

                _elements[i].TargetPosition = GlobalPosition + new Vector2(xPos, yPos);
                _elements[i].TargetSize = new Vector2(GetWindow().Size.X * 2, segmentHeight);
            }
        }
    }
}
