using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace jacket.Reporting
{
    class DetailsReporter : ConsoleReporter
    {
        const char FAIL = '×';
        const char VERTICAL = '│';
        const char HORIZONTAL = '─';
        const char DOWNRIGHT = '┌';
        const char DOWNLEFT = '┐';
        const char UPRIGHT = '└';
        const char UPLEFT = '┘';
        const char VERTICALRIGHT = '├';
        const char VERTICALLEFT = '┤';
        const char DOWNHORIZONTAL = '┬';
        const char UPHORIZONTAL = '┴';
        const char VERTICALHORIZONTAL = '┼';

        protected override void OnSuccess(ScenarioResult scenarioResult)
        {
            PrintGivenWhenThen(scenarioResult);
        }

        void PrintGivenWhenThen(ScenarioResult scenarioResult)
        {
            if (scenarioResult.Metadata.Any() == false) return;
            Console.WriteLine(scenarioResult.Metadata.ScenarioName());
            PrintGiven(scenarioResult, "given", scenarioResult.Metadata.GivenKeys());
            PrintGiven(scenarioResult, "when", scenarioResult.Metadata.WhenKeys());
            PrintGiven(scenarioResult, "then", scenarioResult.Metadata.ThenKeys());
            Console.WriteLine();
            PrintExamples(scenarioResult);
            Console.WriteLine();
        }

        void PrintExamples(ScenarioResult scenarioResult)
        {
            var exampleResults = scenarioResult as ExamplesResult;
            if (exampleResults == null) return;

            //HORRIBLE CODE AHEAD.
            var headers =
                (from example in
                     exampleResults.Examples.SelectMany(_ => _.Values,
                                                        (scenario, kv) => new { kv.Key, Value = kv.Value.ToString() })
                 group example by example.Key
                     into byKey
                     select new { Title = byKey.Key, MaxLength = Math.Max(byKey.Key.Length,byKey.Max(_ => _.Value.Length)) })
                    .ToDictionary(_ => _.Title, _ => _.MaxLength);
            Console.WriteLine();
            var separatorsData = headers.Select((_, index) => Tuple.Create(index, headers.Count, new String(HORIZONTAL, headers[_.Key]), headers[_.Key])).ToList();
            var headersData = headers.Select((_, index) => Tuple.Create(index, headers.Count, _.Key, headers[_.Key]));

            PrintTableLine(GetTableLine(separatorsData, DOWNRIGHT, DOWNHORIZONTAL, DOWNLEFT));
            PrintTableLine(GetTableLine(headersData, VERTICAL, VERTICAL, VERTICAL));
            Action writeSeparatorLine = () => PrintTableLine(GetTableLine(separatorsData, VERTICALRIGHT, VERTICALHORIZONTAL, VERTICALLEFT));
            writeSeparatorLine();

            foreach (var example in exampleResults.Examples.Select((_, index) => Tuple.Create(_, index)))
            {
                var dataPoints = example.Item1.Values.Select((_, index) => Tuple.Create(index, headers.Count, _.Value.ToString(), headers[_.Key]));
                PrintTableLine(GetTableLine(dataPoints, VERTICAL, VERTICAL, VERTICAL, 
                    example.Item1.Result.Result == ScenarioResult.SUCCESS ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
                if (example.Item2 == example.Item2 - 1)
                    writeSeparatorLine();
            }
            PrintTableLine(GetTableLine(separatorsData, UPRIGHT, UPHORIZONTAL, UPLEFT));
            
        }

        void PrintTableLine(IEnumerable<Cell> line)
        {
            foreach (var cell in line) WriteCell(cell);
            
            Console.WriteLine();
        }

        static void WriteCell(Cell cell)
        {
            if (cell.Prefix != '\0')
                Console.Write(cell.Prefix);
            using(ConsoleColorizer.Colorize(cell.Color))
                Console.Write(cell.Content);
            if (cell.Suffix != '\0')
                Console.Write(cell.Suffix);
        }

        static IEnumerable<Cell> GetTableLine(IEnumerable<Tuple<int, int, string, int>> cellDataPoints, char firstSeparator, char separator, char lastSeparator,ConsoleColor? color = null)
        {
            return cellDataPoints.Select(pair => new Cell
            {
                Prefix = pair.Item1 == 0 ? firstSeparator : separator,
                Content = pair.Item3.PadLeft(pair.Item4),
                Suffix = pair.Item1 == pair.Item2 - 1 ? lastSeparator : '\0',
                Color = color
            });
        }

        class Cell
        {
            public char Prefix = '\0';
            public string Content;
            public char Suffix = '\0';
            public ConsoleColor? Color { get; set; }
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
            using (ConsoleColorizer.Colorize(GetResultColor(scenarioResult, prefix, key)))
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
                && scenarioResult.Metadata.Result(prefix, key) == "fail" ? FAIL : ' ';
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
        public override void OnStart()
        {
            Console.WriteLine("Starting testing.");
        }
    }
}