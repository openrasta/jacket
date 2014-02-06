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

            _instance = Activator.CreateInstance(_type);
            _testMethod = GetTestMethod();
            SetIntrospection();
            return Task.FromResult(this);
        }

        Task GetTestMethod()
        {
            return new Task(()=> _type.GetMethod(_methodDefinition.Name, 
                                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                      .Invoke(_instance, new object[0]));
        }

        public Task<ScenarioResult> Run()
        {
            try
            {
                _testMethod.RunSynchronously();
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

        Task<ScenarioResult> ResultIsSuccess()
        {
            SetResultOnIntrospectionData(SUCCESS);
            return Task.FromResult(new ScenarioResult() { Result = SUCCESS });
        }

        Task<ScenarioResult> ResultIsFault(Exception exception)
        {
            SetResultOnIntrospectionData(FAIL);
            return Task.FromResult(new ScenarioResult { Result = FAIL });
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