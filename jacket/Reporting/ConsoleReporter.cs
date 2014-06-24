using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace jacket.Reporting
{
    public abstract class ConsoleReporter : IReporter
    {
        static readonly Regex EM = new Regex(@"\*\w([\w|\s]*\w)?\*");

        static readonly Dictionary<ConsoleColor, ConsoleColor> HIGHLIGHTS = new Dictionary<ConsoleColor, ConsoleColor>
        {
            { ConsoleColor.Gray, ConsoleColor.White },
            { ConsoleColor.DarkGray, ConsoleColor.Gray },
            { ConsoleColor.DarkGreen, ConsoleColor.Green },
            { ConsoleColor.DarkRed, ConsoleColor.Red }
        };

        readonly ConcurrentBag<Action> _errorReportWrites = new ConcurrentBag<Action>();
        readonly ConcurrentBag<Action> _writes = new ConcurrentBag<Action>();
        bool _finished;
        bool _started;

        protected ConsoleReporter()
        {
            Result = "success";
            Monitor.Enter(_writes);
        }

        public int FailedAssertions { get; set; }

        public int FailedScenarios { get; set; }

        public string Result { get; private set; }
        public int SuccessfulAssertions { get; set; }
        public int TotalAssertions { get; set; }

        public int TotalScenarios { get; set; }

        public virtual void OnFinish()
        {
            var success = Result == "success";
            using (ConsoleColorizer.Colorize(success ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed))
            {
                Console.WriteLine(success
                                      ? string.Format(
                                                      "Ran {0} scenarios totaling {1} assertions and completed successfully.",
                                                      TotalScenarios,
                                                      TotalAssertions)
                                      : GetFailedReport());
                Console.WriteLine();
                WriteExceptionReport();
            }
        }

        public abstract void OnStart();

        public void Fail(ScenarioResult scenarioResult)
        {
            Result = "fail";
            UpdateStats(scenarioResult);

            FailedScenarios ++;

            WriteToQueue(() => OnFail(scenarioResult));
        }

        public void Finished()
        {
            _errorReportWrites.Add(OnFinish);
            _finished = true;
        }


        public void RunUntilCompletion()
        {
            WaitUntilFinishedOrQueueEmpty(_writes);
            WaitUntilFinishedOrQueueEmpty(_errorReportWrites);
        }

        public void Success(ScenarioResult scenarioResult)
        {
            if (!_started) OnStart();
            UpdateStats(scenarioResult);

            _started = true;
            WriteToQueue(() => OnSuccess(scenarioResult));
        }

        protected abstract void OnFail(ScenarioResult scenarioResult);
        protected abstract void OnSuccess(ScenarioResult scenarioResult);

        protected void WriteMarkdownLine(string line, params object[] parameters)
        {
            line = string.Format(line, parameters);

            var matches = EM.Matches(line).OfType<Match>();
            if (matches.Any() == false)
            {
                Console.WriteLine(line);
                return;
            }
            var pos = 0;
            foreach (var match in matches)
            {
                var prefixLength = match.Index - pos;
                Console.Write(line.Substring(pos, prefixLength));
                pos = match.Index;
                using (ConsoleColorizer.Colorize(GetHighlightColor()))
                {
                    Console.Write(line.Substring(pos + 1, match.Length - 2));
                    pos += match.Length;
                }
            }
            if (pos < line.Length)
                Console.Write(line.Substring(pos));
            Console.WriteLine();
        }

        string GetFailedReport()
        {
            var unusedAsserts = TotalAssertions - (FailedAssertions + SuccessfulAssertions);
            return string.Format("Ran {0} scenario{5}, {1} failed." + Environment.NewLine +
                                 " - {2} assertions failed" + Environment.NewLine +
                                 " - {3} not executed" + Environment.NewLine +
                                 " - {4} successful.",
                                 TotalScenarios,
                                 FailedScenarios,
                                 FailedAssertions,
                                 unusedAsserts,
                                 SuccessfulAssertions,
                                 TotalScenarios == 1 ? "" : "s");
        }

        ConsoleColor? GetHighlightColor()
        {
            ConsoleColor returnValue;
            return HIGHLIGHTS.TryGetValue(Console.ForegroundColor, out returnValue) ? returnValue : ConsoleColor.Gray;
        }

        void UpdateStats(ScenarioResult scenarioResult)
        {
            TotalScenarios++;
            var metadata = scenarioResult.Metadata;
            TotalAssertions += metadata.ThenKeys().Count();
            var results = metadata.ThenKeys().Select(key => metadata.Result("then", key)).ToList();
            FailedAssertions += results.Count(_ => _ == "fail");
            SuccessfulAssertions += results.Count(_ => _ == "success");
        }

        void WaitUntilFinishedOrQueueEmpty(ConcurrentBag<Action> queue)
        {
            while (!_finished || !queue.IsEmpty)
            {
                Action writer;
                if (queue.TryTake(out writer))
                    writer();
                else
                    Monitor.Wait(queue);
            }
        }

        void WriteExceptionReport()
        {
        }

        void WriteToQueue(Action action)
        {
            _writes.Add(action);
            Monitor.PulseAll(_writes);
        }
    }
}
