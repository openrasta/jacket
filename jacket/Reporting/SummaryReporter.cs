using System;

namespace jacket.Reporting
{
    public class SummaryReporter : ConsoleReporter
    {
        protected override void OnSuccess(ScenarioResult scenarioResult)
        {
            Console.WriteLine(".");
        }

        protected override void OnFail(ScenarioResult scenarioResult)
        {
            Console.WriteLine("F");
        }
    }
}