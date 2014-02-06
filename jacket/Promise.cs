using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jacket
{
    public class Promise<T, TResult> : IPromise<T,TResult>
    {
        readonly IEnumerable<Task<T>> _stuff;
        readonly Func<T, TResult> _discriminator;
        readonly List<PromiseEventHandler> _handlers =  new List<PromiseEventHandler>();

        public Promise(IEnumerable<Task<T>> stuff,Func<T,TResult> discriminator)
        {
            _stuff = stuff;
            _discriminator = discriminator;
        }

        public Promise<T,TResult> On(TResult @event, Action<object> doSomething)
        {
            _handlers.Add(new PromiseEventHandler(@event, doSomething));

            return this;
        }

        public async Task Start()
        {
            var lookup = _handlers.ToLookup(_ => _.Event);
            foreach (var element in _stuff)
            {
                var executed = await element;
                var lookupKey = _discriminator(executed);
                foreach (var handler in lookup[lookupKey])
                    handler.DoSomething(new object());
            }
            
        }
        class PromiseEventHandler{
            public readonly TResult Event;
            public readonly Action<object> DoSomething;

            public PromiseEventHandler(TResult @event, Action<object> doSomething)
            {
                Event = @event;
                DoSomething = doSomething;
            }
        }
    }
}