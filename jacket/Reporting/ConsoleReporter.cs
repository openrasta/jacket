using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace jacket.Reporting
{
    public abstract class ConsoleReporter : IReporter
    {
        readonly ConcurrentBag<Action> _writes = new ConcurrentBag<Action>();
        bool _finished;
        bool _started;
        readonly ConcurrentBag<Action> _errorReportWrites = new ConcurrentBag<Action>();

        protected ConsoleReporter()
        {
            Result = "success";
            Monitor.Enter(_writes);
        }

        public string Result { get; private set; }

        public void Success(ScenarioResult scenarioResult)
        {
            if (!_started) OnStart();
            UpdateStats(scenarioResult);

            _started = true;
            WriteToQueue(() => OnSuccess(scenarioResult));
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

        public abstract void OnStart();

        public void Fail(ScenarioResult scenarioResult)
        {
            Result = "fail";
            UpdateStats(scenarioResult);

            FailedScenarios ++;

            WriteToQueue(() => OnFail(scenarioResult));
        }

        void WriteToQueue(Action action)
        {
            _writes.Add(action);
            Monitor.PulseAll(_writes);
        }


        public void RunUntilCompletion()
        {
            WaitUntilFinishedOrQueueEmpty(_writes);
            WaitUntilFinishedOrQueueEmpty(_errorReportWrites);
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

        protected abstract void OnSuccess(ScenarioResult scenarioResult);
        protected abstract void OnFail(ScenarioResult scenarioResult);

        public void Finished()
        {
            _errorReportWrites.Add(OnFinish);
            _finished = true;
        }

        public virtual void OnFinish()
        {
            var success = Result == "success";
            using (ConsoleColorizer.Colorize(success ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed))
            {
                Console.WriteLine(success
                        ? string.Format("Ran {0} scenarios totaling {1} assertions and completed successfully.", TotalScenarios, TotalAssertions)
                        : GetFailedReport());
            }
        }

        string GetFailedReport()
        {
            int unusedAsserts = TotalAssertions - (FailedAssertions + SuccessfulAssertions);
            return string.Format("Ran {0} scenario{5}, {1} failed." + Environment.NewLine + 
                                 " - {2} assertions failed" + Environment.NewLine +
                                 " - {3} not executed"  + Environment.NewLine +
                                 " - {4} successful.",
                                 TotalScenarios,
                                 FailedScenarios, 
                                 FailedAssertions, 
                                 unusedAsserts, 
                                 SuccessfulAssertions,
                                 TotalScenarios == 1 ? "" : "s");
        }

        public int SuccessfulAssertions { get; set; }

        public int FailedAssertions { get; set; }

        public int FailedScenarios { get; set; }

        public int TotalAssertions { get; set; }

        public int TotalScenarios { get; set; }
    }
}