using System;

namespace Scripts.Utils
{
    /// <summary>
    /// Common validation utilities to reduce code duplication across the codebase
    /// </summary>
    public static class CommonValidation
    {
        /// <summary>
        /// Validates that a value is not null and throws ArgumentNullException if it is
        /// </summary>
        /// <typeparam name="T">The type of the value to validate</typeparam>
        /// <param name="value">The value to validate</param>
        /// <param name="paramName">The parameter name for the exception</param>
        /// <returns>The validated value</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static T ThrowIfNull<T>(T value, string paramName) where T : class
        {
            return value ?? throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Validates that a string is not null or empty and throws ArgumentException if it is
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="paramName">The parameter name for the exception</param>
        /// <returns>The validated string</returns>
        /// <exception cref="ArgumentException">Thrown when value is null or empty</exception>
        public static string ThrowIfNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty", paramName);
            return value;
        }

        /// <summary>
        /// Validates that a collection is not null or empty
        /// </summary>
        /// <typeparam name="T">The type of the collection elements</typeparam>
        /// <param name="collection">The collection to validate</param>
        /// <param name="paramName">The parameter name for the exception</param>
        /// <returns>The validated collection</returns>
        /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
        public static System.Collections.Generic.IEnumerable<T> ThrowIfNullOrEmpty<T>(
            System.Collections.Generic.IEnumerable<T> collection, 
            string paramName)
        {
            if (collection == null)
                throw new ArgumentNullException(paramName);
            
            using var enumerator = collection.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new ArgumentException("Collection cannot be empty", paramName);
            
            return collection;
        }

        /// <summary>
        /// Validates that an index is within valid range for a collection
        /// </summary>
        /// <param name="index">The index to validate</param>
        /// <param name="collectionSize">The size of the collection</param>
        /// <param name="paramName">The parameter name for the exception</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range</exception>
        public static void ThrowIfIndexOutOfRange(int index, int collectionSize, string paramName)
        {
            if (index < 0 || index >= collectionSize)
                throw new ArgumentOutOfRangeException(paramName,
                    $"Index {index} is out of range for collection of size {collectionSize}");
        }
    }

    /// <summary>
    /// Extension methods for validation to maintain backward compatibility
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Extension method for validating not null - delegates to CommonValidation
        /// </summary>
        public static T ValidateNotNull<T>(this T value, string paramName) where T : class
            => CommonValidation.ThrowIfNull(value, paramName);

        /// <summary>
        /// Extension method for validating index - delegates to CommonValidation
        /// </summary>
        public static void ValidateIndex(this int index, int collectionSize, string paramName)
            => CommonValidation.ThrowIfIndexOutOfRange(index, collectionSize, paramName);
    }
}