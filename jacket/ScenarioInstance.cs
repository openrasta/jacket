using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;

namespace jacket
{
    public class ScenarioInstance
    {
        const string FAIL = "fail";
        const string SUCCESS = "success";
        readonly string _typeName;
        readonly string _assembly;
        readonly IDictionary<string, object> _introspection;
        readonly MethodDefinition _methodDefinition;
        object _instance;
        Type _type;
        Task _testMethod;
        bool _failed;

        public ScenarioInstance(string typeName, string assembly, IDictionary<string, object> introspection, MethodDefinition methodDefinition)
        {
            _typeName = typeName;
            _assembly = assembly;
            _introspection = introspection;
            _methodDefinition = methodDefinition;
        }

        public Task<ScenarioInstance> Initialize()
        {
            // hacky and slow but we don't care too much yet do we?
            _type = Type.GetType(_typeName + ", " + _assembly);
            try
            {
                _instance = Activator.CreateInstance(_type);
                if (_instance == null)
                    throw new NotSupportedException("test class cannot be instantiated");
                WriteGivenWhensSucceeded();
                _testMethod = GetTestMethod();
                SetIntrospection();
            }
            catch (Exception e)
            {
                WriteExceptionToIntrospectionDetails(e);
            }
            return Task.FromResult(this);
        }

        void WriteGivenWhensSucceeded()
        {
            foreach (var key in _introspection.GivenKeys().Select(_ => Tuple.Create("given", _))
                                              .Concat(_introspection.WhenKeys().Select(_ => Tuple.Create("when", _))))
            {
                _introspection[string.Format("{0}.{1}.result", key.Item1, key.Item2)] = "success";
            }
        }

        void WriteExceptionToIntrospectionDetails(Exception exception)
        {
            _failed = true;
            var stackTrace = exception.ToString();
            var methodNames = _introspection.MethodNames().ToList();

            var selected = from method in methodNames
                           let prefix = method.Item1
                           let key = method.Item2
                           let methodName = method.Item3
                           let index = stackTrace.IndexOf(methodName)
                           where index != -1
                           select new { prefix,key,method, index };

            var lastMethodCalled = selected.OrderByDescending(_=>_.index).First();
            var resultKey = string.Format("{0}.{1}.{2}", lastMethodCalled.prefix, lastMethodCalled.key, "result");
            var errorKey = string.Format("{0}.{1}.{2}", lastMethodCalled.prefix, lastMethodCalled.key, "exception");
            _introspection[resultKey] = "fail";
            _introspection[errorKey] = exception;
        }

        Task GetTestMethod()
        {
            return new Task(()=> _type.GetMethod(_methodDefinition.Name, 
                                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                      .Invoke(_instance, new object[0]));
        }

        public Task<ScenarioResult> Run()
        {
            if (_failed)
                return ResultFaultedAlready();
            try
            {
                _testMethod.RunSynchronously();
                SetResultOnIntrospectionData(SUCCESS);


                IDisposable @finally = _instance as IDisposable;
                if (@finally != null)
                    @finally.Dispose();

                if (_testMethod.IsFaulted)
                {
                    return ResultIsFault(_testMethod.Exception);
                }
            }
            catch (Exception e)
            {
                return ResultIsFault(e);
            }
            return ResultIsSuccess();
        }

        Task<ScenarioResult> ResultFaultedAlready()
        {
            return Task.FromResult(new ScenarioResult(FAIL, _introspection));
        }

        Task<ScenarioResult> ResultIsSuccess()
        {
            SetResultOnIntrospectionData(SUCCESS);
            return Task.FromResult(new ScenarioResult(SUCCESS, _introspection));
        }

        Task<ScenarioResult> ResultIsFault(Exception exception)
        {
            SetResultOnIntrospectionData(FAIL);
            return Task.FromResult(new ScenarioResult(FAIL, _introspection));
        }

        void SetResultOnIntrospectionData(string result)
        {
            _introspection["lastrun.then"] = _methodDefinition.Name;
            _introspection["then." + _methodDefinition.Name + ".result"] = result;
        }

        public async Task<ScenarioInstance> Construct()
        {
            await Initialize();
            return this;
        }

        public void SetIntrospection()
        {
            var field = _type.GetField("scenario_details", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return;
            field.SetValue(_instance, _introspection);
        }
    }
}