using System;
using System.Collections.Generic;

namespace Tests.Scenarios
{
    public class simple_scenario : contexts.@base, IDisposable
    {
        IDictionary<string, object> scenario_details;
        public simple_scenario()
        {
            given_a_given_setup_method();
            
            when_running_a_test();
        }

        void givens_are_correct()
        {
            scenario_details["given"].Is("a_given_setup_method");
            scenario_details["given.a_given_setup_method.key"].Is("a_given_setup_method");
            scenario_details["given.a_given_setup_method.method.name"].Is("given_a_given_setup_method");
            scenario_details["given.a_given_setup_method.display.name"].Is("a given setup method");
        }

        void when_is_correct()
        {
            scenario_details["when"].Is("running_a_test");
            scenario_details["when.running_a_test.key"].Is("running_a_test");
            scenario_details["when.running_a_test.display.name"].Is("running a test");
            scenario_details["when.running_a_test.method.name"].Is("when_running_a_test");
        }

        void thens_are_correct()
        {
            scenario_details["then"].Is("givens_are_correct,when_is_correct,thens_are_correct");
        }

        public void Dispose()
        {
            scenario_details["given.a_given_setup_method.result"].Is("success");
            scenario_details["when.running_a_test.result"].Is("success");
            scenario_details["then." + scenario_details["lastrun.then"] + ".result"].Is("success");
        }
    }

    namespace contexts
    {
        public abstract class @base
        {
            protected void when_running_a_test()
            {
            
            }

            protected void given_a_given_setup_method()
            {
            

            }
        }
    }
}