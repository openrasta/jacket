using System;
using System.Collections.Generic;

namespace Tests
{
    public static class IsExtensions
    {
        public static T Is<T>(this T actual, T expected)
        {
            if (EqualityComparer<T>.Default.Equals(actual,expected) == false)
                throw new AssertionFailedException(actual, expected);
            return actual;
        }

        public static T IsNotNull<T>(this T actual) where T : class
        {
            if (ReferenceEquals(actual, null))
                throw new AssertionFailedException("null", "not null");
            return actual;
        }

        public static T IsOfType<T>(this object actual)
        {
            if (!(actual is T))
                throw new AssertionFailedException("an instance of type " + actual.GetType(),
                                                   "an instance of type " + typeof(T).Name);
            return (T)actual;
        }
    }
}