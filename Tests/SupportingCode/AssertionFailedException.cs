using System;

namespace Tests.SupportingCode
{
    public class AssertionFailedException<T> : Exception
    {
        public AssertionFailedException(T actual, T expected)
            : base(string.Format("Expected value {0} but got {1} instead", expected, actual))
        {
        }
    }
}