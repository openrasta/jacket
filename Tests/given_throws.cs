using System;
using System.Collections.Generic;
using Tests.Annotations;
using Tests.SupportingCode;

namespace Tests
{
    public class given_throws : contexts.@base
    {
        [UsedImplicitly]
        IDictionary<string, object> scenario_details;

        public given_throws()
        {
            given_thing_that_throws();
            when_running_a_test();
        }

        public void given_fails()
        {
            scenario_details["given.thing_that_throws.result"].Is("fail");
        }
    }
}