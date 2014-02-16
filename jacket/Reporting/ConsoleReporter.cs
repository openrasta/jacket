using System;
using System.Collections.Concurrent;
using System.Threading;

namespace jacket.Reporting
{
    public abstract class ConsoleReporter : IReporter
    {
        readonly ConcurrentBag<Action> _writes = new ConcurrentBag<Action>();
        bool _finished;

        protected ConsoleReporter()
        {
            Monitor.Enter(_writes);
        }

        public void Success(ScenarioResult scenarioResult)
        {
            _writes.Add(()=>OnSuccess(scenarioResult));
            Monitor.PulseAll(_writes);
        }

        public void Fail(ScenarioResult scenarioResult)
        {
            _writes.Add(() => OnFail(scenarioResult));
            Monitor.PulseAll(_writes);
        }

        public void RunUntilCompletion()
        {
            while (!_finished || !_writes.IsEmpty)
            {
                Action writer;
                if (_writes.TryTake(out writer))
                    writer();
                else
                    Monitor.Wait(_writes);
            }
        }

        protected abstract void OnSuccess(ScenarioResult scenarioResult);
        protected abstract void OnFail(ScenarioResult scenarioResult);

        public void Finished()
        {
            _finished = true;
        }
    }
}