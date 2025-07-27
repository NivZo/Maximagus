using System;

public static class ValidationExtensions
{
    public static T ValidateNotNull<T>(this T value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
        return value;
    }
    
    public static void ValidateIndex(this int index, int collectionSize, string paramName)
    {
        if (index < 0 || index >= collectionSize)
            throw new ArgumentOutOfRangeException(paramName, $"Index {index} is out of range for collection of size {collectionSize}");
    }
}