using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jacket
{
    public static class OnExtensionMethods
    {
        public static IPromise<T,string> On<T>(this IEnumerable<Task<T>> stuff, string @event, Action<object> action)
            where T:ScenarioResult
        {
            return new Promise<T,string>(stuff, _=>_.Result).On(@event, action);
        }
    }
}