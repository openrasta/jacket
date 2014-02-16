using System;

namespace jacket
{
    public class SummaryReporter : ConsoleReporter
    {
        public override void OnSuccess(ScenarioResult scenarioResult)
        {
            Console.WriteLine(".");
        }

        public override void OnFail(ScenarioResult scenarioResult)
        {
            Console.WriteLine("F");
        }
    }
}