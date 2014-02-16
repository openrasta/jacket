using System;
using System.Threading.Tasks;

namespace jacket
{
    public interface IPromise<T,TResult>
    {
        Task Start(Action finished);
        Promise<T,TResult> On(TResult @event, Action<T> doSomething);
    }
}