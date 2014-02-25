using System;

namespace jacket.Reporting
{
    public class SummaryReporter : ConsoleReporter
    {
        public override void OnStart()
        {
            Console.Write('[');
        }
        protected override void OnSuccess(ScenarioResult scenarioResult)
        {
            Console.Write(".");
        }

        protected override void OnFail(ScenarioResult scenarioResult)
        {
            Console.Write("F");
        }
        public override void OnFinish()
        {
            Console.WriteLine(']');
            base.OnFinish();
        }
    }
}