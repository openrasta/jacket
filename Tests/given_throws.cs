using System;
using System.Collections.Generic;
using Tests.Annotations;
using Tests.SupportingCode;

namespace Tests
{
    public class given_throws : contexts.@base
    {
        public given_throws()
        {
            given_thing_that_throws_not_supported();
            when_running_a_test();
        }

        public void given_fails()
        {
            scenario_details["given.thing_that_throws_not_supported.result"].Is("fail");
            scenario_details["given.thing_that_throws_not_supported.exception"]
                .IsNotNull()
                .IsOfType<NotSupportedException>();
        }
    }
}