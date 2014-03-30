using System.Collections.Generic;
using Tests.Annotations;

namespace Tests.metadata
{
    class constructor_injection_no_name : contexts.@base
    {
        static object[] examples =
        {
            new { ctorValue = "first name"}
        };
        public constructor_injection_no_name(string ctorValue)
        {
            given_constructor_value(ctorValue);
            when_running_a_test();
        }

        void scenario_name_includes_value()
        {
            scenario_details["display.name"].Is("constructor injection no name (#1)");
        }

        void test_assertion_is_called(string ctorValue)
        {
            constructor_value.Is(ctorValue);
        }

        void parametised_test_assertion_contains_name()
        {
            scenario_details["then.test_assertion_is_called.display.name"]
                .Is("test assertion is called with 'first name'");
        }
    }
}