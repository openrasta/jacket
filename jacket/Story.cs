using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;

namespace jacket
{
    public class Story
    {
        public Story(string assemblyFilePath)
        {
            Scenarios = AssemblyDefinition.ReadAssembly(assemblyFilePath)
                                          .MainModule.Types
                                          .Where(IsScenario)
                                          .Select(ToScenario);
        }

        bool IsScenario(TypeDefinition typeDefinition)
        {
            return typeDefinition.IsAbstract == false && typeDefinition.IsClass
                && typeDefinition.IsPublic && !typeDefinition.HasGenericParameters;
        }

        public IEnumerable<Scenario> Scenarios { get; set; }

        Scenario ToScenario(TypeDefinition typeDefinition)
        {
            return new Scenario(typeDefinition, Path.GetFileNameWithoutExtension(typeDefinition.Module.Name));
        }

        public Task Run(Action<object> success, Action<object> fail)
        {
            return Scenarios.Select(_ => _.RunAsync())
                            .On("success", success)
                            .On("fail", fail)
                            .Start();
        }
    }

    public static class OnExtensionMethods
    {
        public static IPromise<T,string> On<T>(this IEnumerable<Task<T>> stuff, string @event, Action<object> action)
            where T:ScenarioResult
        {
            return new Promise<T,string>(stuff, _=>_.Result).On(@event, action);
        }
    }

    public interface IPromise<T,TResult>
    {
        Task Start();
        Promise<T,TResult> On(TResult @event, Action<object> doSomething);
    }

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