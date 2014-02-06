using System;
using System.Threading.Tasks;

namespace jacket
{
    public interface IPromise<T,TResult>
    {
        Task Start();
        Promise<T,TResult> On(TResult @event, Action<object> doSomething);
    }
}