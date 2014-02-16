using System;
using System.Collections.Concurrent;
using System.Threading;

namespace jacket
{
    public abstract class ConsoleReporter
    {
        readonly ConcurrentBag<Action> _writes = new ConcurrentBag<Action>();
        bool _finished = false;

        public ConsoleReporter()
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

        public abstract void OnSuccess(ScenarioResult scnearioResult);
        public abstract void OnFail(ScenarioResult scnearioResult);

        public void Finished()
        {
            _finished = true;
        }
    }
}