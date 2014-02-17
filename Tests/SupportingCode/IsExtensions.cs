using System;
using System.Collections.Generic;

namespace Tests.SupportingCode
{
    public static class IsExtensions
    {
        public static void Is<T>(this T actual, T expected)
        {
            if (EqualityComparer<T>.Default.Equals(actual,expected) == false)
                throw new AssertionFailedException<T>(actual, expected);
        }
    }
}