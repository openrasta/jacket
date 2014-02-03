using System;
using System.Collections.Generic;

namespace Tests
{
    public static class IsExtensions
    {
        public static void Is<T>(this T actual, T expected)
        {
            if (EqualityComparer<T>.Default.Equals(actual,expected) == false)
                throw new AssertionFailedException<T>(actual, expected);
        }
    }

    public class AssertionFailedException<T> : Exception
    {
        public AssertionFailedException(T actual, T expected)
            : base(string.Format("Expected value {0} but got {1} instead", expected, actual))
        {
        }
    }
}