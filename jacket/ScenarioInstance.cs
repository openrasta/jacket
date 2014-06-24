using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Tests.contexts;

namespace jacket
{
    public class ScenarioInstance
    {
        const string FAIL = "fail";
        const string SUCCESS = "success";
        readonly Scenario _scenarioDefinition;
        readonly string _typeName;
        readonly FileInfo _assemblyFilePath;
        readonly IDictionary<string, object> _introspection;
        readonly MethodDefinition _ctorMethodDefinition;
        readonly IDictionary<string, object> _ctorArgs;
        readonly Lazy<Type> _clrType;
        readonly MethodDefinition _testMethodDefinition;
        object _instance;
        Task _testMethod;
        bool _failed;

        public ScenarioInstance(Scenario scenarioDefinition, IDictionary<string, object> introspection, FileInfo assemblyFilePath, string typeName, MethodDefinition ctor, IDictionary<string, object> ctorArgs, Lazy<Type> clrType, MethodDefinition testMethodDefinition)
        {
            _scenarioDefinition = scenarioDefinition;
            _typeName = typeName;
            _assemblyFilePath = assemblyFilePath;
            _introspection = introspection;
            _ctorMethodDefinition = ctor;
            _ctorArgs = ctorArgs;
            _clrType = clrType;
            _testMethodDefinition = testMethodDefinition;
        }

        public Task<ScenarioInstance> Initialize()
        {
            // hacky and slow but we don't care too much yet do we?

            if (_clrType.Value == null)
                throw new InvalidOperationException(string.Format("Cant find {0} in assembly {1}", _typeName, _assemblyFilePath));
            try
            {
                _instance = FormatterServices.GetUninitializedObject(_clrType.Value);
               _clrType.Value.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                             .InvokeMatching(_instance, _ctorArgs);
                //ctor.Invoke(_instance, new object[0]);
                if (_instance == null)
                    throw new NotSupportedException("test class cannot be instantiated");
                WriteGivenWhensSucceeded();
                
            }
            catch (Exception e)
            {
                e = UnwrapWrapperException(e);
                var method = WriteExceptionToIntrospectionDetailsAndReturnFailingMethodName(e);

                var prefixedKey = string.Format("{0}.{1}", method.Item1, method.Item2);

                var resultKey = string.Format("{0}.{1}", prefixedKey, "result");
                var exceptionKey = string.Format("{0}.{1}", prefixedKey, "exception");
                _introspection[resultKey] = "fail";
                _introspection[exceptionKey] = e;

                _failed = !IsFailureExpected(method.Item3);
            }
            finally
            {
                _testMethod = _failed ? Task.FromResult(-1) : RunTestMethodAsync();
                SetIntrospection();
            }
            return Task.FromResult(this);
        }

        Exception UnwrapWrapperException(Exception exception)
        {
            var tie = exception as TargetInvocationException;
            return tie == null ? exception : tie.InnerException;
        }

        bool IsFailureExpected(string methodName)
        {
            return (from type in _testMethodDefinition.DeclaringType.SelfAndParents()
                   let td = type.Resolve()
                   from method in td.GetMethods()
                   where method.Name == methodName
                   from attribute in method.CustomAttributes
                   where attribute.AttributeType.Name.StartsWith("ExpectedToFail")
                   select attribute).Any();
        }

        void WriteGivenWhensSucceeded()
        {
            foreach (var key in _introspection.GivenKeys().Select(_ => Tuple.Create("given", _))
                                              .Concat(_introspection.WhenKeys().Select(_ => Tuple.Create("when", _))))
            {
                _introspection[string.Format("{0}.{1}.result", key.Item1, key.Item2)] = "success";
            }
        }

        Tuple<string, string, string> WriteExceptionToIntrospectionDetailsAndReturnFailingMethodName(Exception exception)
        {
            var stackTrace = exception.ToString();
            var methodNames = _introspection.GivenWhenThenMethodNames().ToList();

            var selected = from method in methodNames
                           let index = stackTrace.IndexOf(method.MethodName, StringComparison.Ordinal)
                           where index != -1
                           orderby index descending
                               select Tuple.Create(method.Prefix, method.Key, method.MethodName);

            return selected.First();
        }

        Task RunTestMethodAsync()
        {
            return new Task(() =>
                            _clrType.Value.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Where(_ => _.Name == _testMethodDefinition.Name)
                                    .InvokeMatching(_instance, _ctorArgs));
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
            SetResultOnIntrospectionData(FAIL, exception);
            return Task.FromResult(new ScenarioResult(FAIL, _introspection));
        }

        void SetResultOnIntrospectionData(string result, Exception exception = null)
        {
            _introspection["lastrun.then"] = _testMethodDefinition.Name;
            _introspection["then." + _testMethodDefinition.Name + ".result"] = result;
            if (exception != null)
                _introspection["then." + _testMethodDefinition.Name + ".exception"] = exception;
        }

        public async Task<ScenarioInstance> Construct()
        {
            await Initialize();
            return this;
        }

        public void SetIntrospection()
        {
            var field = _clrType.Value.GetField("scenario_details", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return;
            field.SetValue(_instance, _introspection);
        }
    }
}