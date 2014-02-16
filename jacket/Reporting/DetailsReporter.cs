﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace jacket.Reporting
{
    class DetailsReporter : ConsoleReporter
    {
        protected override void OnSuccess(ScenarioResult scenarioResult)
        {
            PrintGivenWhenThen(scenarioResult);
        }

        void PrintGivenWhenThen(ScenarioResult scenarioResult)
        {
            PrintGiven(scenarioResult, "given", scenarioResult.Metadata.GivenKeys());
            PrintGiven(scenarioResult, "when", scenarioResult.Metadata.WhenKeys());
            PrintGiven(scenarioResult, "then", scenarioResult.Metadata.ThenKeys());
        }

        void PrintGiven(ScenarioResult scenarioResult, string prefix, IEnumerable<string> givenKeys)
        {
            var allGivenKeys = givenKeys as IList<string> ?? givenKeys.ToList();
            PrintFirstLanguageItem(scenarioResult, prefix, allGivenKeys.First());
            foreach (var given in allGivenKeys.Skip(1))
                PrintAndLanguageItem(scenarioResult, prefix, given);
        }

        void PrintAndLanguageItem(ScenarioResult scenarioResult, string prefix, string key)
        {
            Console.WriteLine("  {0} {1}", "And", scenarioResult.Metadata.DisplayName(prefix, key));
        }

        void PrintFirstLanguageItem(ScenarioResult scenarioResult, string prefix, string key)
        {
            Console.WriteLine("{0} {1}", prefix.Capitalize().PadLeft(5), scenarioResult.Metadata.DisplayName(prefix, key));
        }

        protected override void OnFail(ScenarioResult scenarioResult)
        {
        }
    }
}