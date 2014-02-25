using System;

namespace Tests.SupportingCode
{
    public class AssertionFailedException : Exception
    {
        public AssertionFailedException(object actual, object expected)
            : base(string.Format("Expected {0} but got {1} instead", expected, actual))
        {
        }
    }
}