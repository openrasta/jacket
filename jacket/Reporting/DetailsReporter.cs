using System;
using System.Collections.Generic;
using System.Linq;

namespace jacket.Reporting
{
    class DetailsReporter : ConsoleReporter
    {
        const char FAIL = '×';
        protected override void OnSuccess(ScenarioResult scenarioResult)
        {
            PrintGivenWhenThen(scenarioResult);
        }

        void PrintGivenWhenThen(ScenarioResult scenarioResult)
        {
            Console.WriteLine(scenarioResult.Metadata["display.name"]);
            PrintGiven(scenarioResult, "given", scenarioResult.Metadata.GivenKeys());
            PrintGiven(scenarioResult, "when", scenarioResult.Metadata.WhenKeys());
            PrintGiven(scenarioResult, "then", scenarioResult.Metadata.ThenKeys());
            Console.WriteLine();
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
            using(ConsoleColorizer.Colorize(GetResultColor(scenarioResult, prefix, key)))
                Console.WriteLine(" {0}   {1} {2}", GetSuccessCharacter(scenarioResult, prefix, key), "and", scenarioResult.Metadata.DisplayName(prefix, key));
        }

        ConsoleColor? GetResultColor(ScenarioResult scenarioResult, string prefix, string key)
        {
            var index = prefix + '.' + key + ".result";
            if (scenarioResult.Metadata.ContainsKey(index) == false) return null;
            return scenarioResult.Metadata[index] == "success" ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
        }

        static char GetSuccessCharacter(ScenarioResult scenarioResult, string prefix, string key)
        {
            return scenarioResult.Metadata.ContainsKey(string.Format("{0}.{1}.result", prefix, key))
                && scenarioResult.Metadata.Result(prefix,key) == "fail" ? FAIL : ' ';
        }

        void PrintFirstLanguageItem(ScenarioResult scenarioResult, string prefix, string key)
        {
            using (ConsoleColorizer.Colorize(GetResultColor(scenarioResult, prefix, key)))
                Console.WriteLine(" {0} {1} {2}", GetSuccessCharacter(scenarioResult, prefix, key), prefix.Capitalize().PadLeft(5), scenarioResult.Metadata.DisplayName(prefix, key));
        }

        protected override void OnFail(ScenarioResult scenarioResult)
        {
            PrintGivenWhenThen(scenarioResult);
        }
    }
}