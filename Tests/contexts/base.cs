using System;
using System.Collections.Generic;
using Tests.Annotations;

namespace Tests.contexts
{
    public abstract class @base
    {
        [UsedImplicitly] protected IDictionary<string, object> scenario_details;
        protected string constructor_value;

        protected void when_running_a_test()
        {
            
        }

        protected void given_a_given_setup_method()
        {
        }

        [ExpectedToFail]
        protected void given_thing_that_throws_not_supported()
        {
            throw new NotSupportedException();
        }

        protected void given_nothing()
        {
            
        }

        protected void given_constructor_value(string ctorValue)
        {
            constructor_value = ctorValue;
        }
    }

    public class ExpectedToFailAttribute : Attribute
    {
    }
}
