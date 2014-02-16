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


        public Promise<T,TResult> On(TResult @event, Action<T> doSomething)
        {
            _handlers.Add(new PromiseEventHandler(@event, doSomething));

            return this;
        }

        public async Task Start(Action finished)
        {
            var lookup = _handlers.ToLookup(_ => _.Event);
            foreach (var element in _stuff)
            {
                var result = await element;
                var lookupKey = _discriminator(result);
                foreach (var handler in lookup[lookupKey])
                    handler.DoSomething(result);
            }
            finished();
        }
        class  PromiseEventHandler{
            public readonly TResult Event;
            public readonly Action<T> DoSomething;

            public PromiseEventHandler(TResult @event, Action<T> doSomething)
            {
                Event = @event;
                DoSomething = doSomething;
            }
        }
    }
}