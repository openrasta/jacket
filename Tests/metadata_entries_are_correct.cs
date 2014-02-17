using System;
using System.Collections.Generic;
using Tests.Annotations;
using Tests.contexts;
using Tests.SupportingCode;

namespace Tests
{
    public class metadata_entries_are_correct : @base, IDisposable
    {
        [UsedImplicitly] IDictionary<string, object> scenario_details;
        public metadata_entries_are_correct()
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
}