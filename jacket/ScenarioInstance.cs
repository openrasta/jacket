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
        readonly MethodDefinition _methodDefinition;
        object _instance;
        Type _type;
        Task _testMethod;
        bool _failed;

        public ScenarioInstance(Scenario scenarioDefinition, string typeName, FileInfo assemblyFilePath, IDictionary<string, object> introspection, MethodDefinition methodDefinition)
        {
            _scenarioDefinition = scenarioDefinition;
            _typeName = typeName;
            _assemblyFilePath = assemblyFilePath;
            _introspection = introspection;
            _methodDefinition = methodDefinition;
        }

        public Task<ScenarioInstance> Initialize()
        {
            // hacky and slow but we don't care too much yet do we?
            var assembly = Assembly.LoadFrom(_assemblyFilePath.FullName);
            _type = assembly.GetType(_typeName);
            if (_type == null)
                throw new InvalidOperationException(string.Format("Cant find {0} in assembly {1}", _typeName, _assemblyFilePath));
            bool runTestMethod = true;
            try
            {
                _instance = FormatterServices.GetUninitializedObject(_type);
                var ctor = _type.GetConstructor(new Type[0]);
                ctor.Invoke(_instance, new object[0]);
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
            return (from type in _methodDefinition.DeclaringType.SelfAndParents()
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
            var methodNames = _introspection.MethodNames().ToList();

            var selected = from method in methodNames
                           let prefix = method.Item1
                           let key = method.Item2
                           let methodName = method.Item3
                           let index = stackTrace.IndexOf(methodName, StringComparison.Ordinal)
                           where index != -1
                           orderby index descending
                               select Tuple.Create(prefix,key,methodName);

            return selected.First();
        }

        Task RunTestMethodAsync()
        {
            return new Task(()=> GetReflectionMethodInfoForTest()
                                      .Invoke(_instance, new object[0]));
        }

        MethodInfo GetReflectionMethodInfoForTest()
        {
            return _type.GetMethod(_methodDefinition.Name, 
                                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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