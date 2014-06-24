using System;

namespace Tests
{
    public class singleword : contexts.@base, IDisposable
    {
        public singleword()
        {
            given_a_given_setup_method();
            when_running_a_test();
        }

        public void Dispose()
        {
            scenario_details["then.all_went_ok_without_the_underscore.result"].Is("success");
        }

        void all_went_ok_without_the_underscore()
        {
        }
    }
}
