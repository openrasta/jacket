using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace jacket.Reporting
{
    public abstract class ConsoleReporter : IReporter
    {
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
            bool success = Result == "success";
            using (ConsoleColorizer.Colorize(success ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed))
            {
                Console.WriteLine(success
                                      ? string.Format("Ran {0} scenarios totaling {1} assertions and completed successfully.",
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

        string GetFailedReport()
        {
            int unusedAsserts = TotalAssertions - (FailedAssertions + SuccessfulAssertions);
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

        void UpdateStats(ScenarioResult scenarioResult)
        {
            TotalScenarios++;
            IDictionary<string, object> metadata = scenarioResult.Metadata;
            TotalAssertions += metadata.ThenKeys().Count();
            List<string> results = metadata.ThenKeys().Select(key => metadata.Result("then", key)).ToList();
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
