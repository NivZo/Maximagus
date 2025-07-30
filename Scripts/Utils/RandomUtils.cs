using System;
using System.Collections.Generic;
using Godot;

public static class RandomUtils
{
    private static Random _rnd = new();

    public static float RandomInRange(float min, float max)
        => (float)_rnd.NextDouble() * (max - min) + min;

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = _rnd.Next(i + 1);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        return list;
    }
    
    public static T RandomElement<T>(this IList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            throw new ArgumentException("List cannot be null or empty.", nameof(list));
        }
        
        return list[_rnd.Next(list.Count)];
    }
}