using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace jacket
{
    public class Scenario
    {
        readonly TypeDefinition _typeDefinition;
        readonly string _assembly;

        public Scenario(TypeDefinition typeDefinition, string assembly)
        {
            _typeDefinition = typeDefinition;
            _assembly = assembly;
        }

        public async Task<ScenarioResult> RunAsync()
        {
            var instance = await Construct();
            var introspect = GetIntrospectionDetails();
            instance.SetIntrospection(introspect);

            return await instance.Run();
        }

        IDictionary<string, object> GetIntrospectionDetails()
        {
            var methodCallsInConstructor = _typeDefinition.GetConstructors().Single(_ => _.Parameters.Count == 0)
                                             .Body.Instructions.Where(_ => _.OpCode == OpCodes.Call)
                                             .Select(_=>_.Operand).OfType<MethodReference>()
                                             .ToList();

            var introspectionDetails = new Dictionary<string, object>();
            AddLanguageElements("given", methodCallsInConstructor, introspectionDetails);
            AddLanguageElements("when", methodCallsInConstructor, introspectionDetails);

            return introspectionDetails;
        }

        void AddLanguageElements(string prefix, IEnumerable<MethodReference> methodCallsInConstructor, IDictionary<string, object> introspectionDetails)
        {
            var allKeys = new StringBuilder();
            foreach (var method in methodCallsInConstructor.Where(_ => _.Name.StartsWith(prefix))
                                                           .Select(AsLanguageElement))
            {
                allKeys.AppendIfNotEmpty(",").Append(method.Key);
                introspectionDetails.Add(string.Format("{0}.{1}.key",prefix, method.Key), method.Key);
                introspectionDetails.Add(string.Format("{0}.{1}.display.name", prefix, method.Key), method.DisplayName);
                introspectionDetails.Add(string.Format("{0}.{1}.method.name", prefix, method.Key), method.MethodName);
            }
            introspectionDetails.Add(prefix, allKeys.ToString());
        }

        LanguageElement AsLanguageElement(MethodReference method)
        {
            var nameWithoutPrefix = method.Name.Substring(method.Name.IndexOf("_")+1);
            return new LanguageElement()
                   {
                       MethodName = method.Name,
                       DisplayName = nameWithoutPrefix.Replace("_", " "),
                       Key = nameWithoutPrefix
                   };
        }

        Task<ScenarioInstance> Construct()
        {
            return new ScenarioInstance(_typeDefinition.FullName, _assembly).Construct();
        }
    }

    public static class StringExtensions
    {
        public static string AsCommaSeparated(this IEnumerable<string> values)
        {
            return values.Aggregate(new StringBuilder(), (sb, value) => sb.AppendIfNotEmpty(",").Append(value)).ToString();
        }

        public static StringBuilder AppendIfNotEmpty(this StringBuilder builder, string value)
        {
            return builder.Length == 0 ? builder : builder.Append(value);
        }
    }

    class LanguageElement
    {
        public string MethodName { get; set; }
        public string DisplayName { get; set; }
        public string Key { get; set; }
    }

    public class ScenarioInstance
    {
        readonly string _typeName;
        readonly string _assembly;
        object _instance;
        Type _type;

        public ScenarioInstance(string typeName, string assembly)
        {
            _typeName = typeName;
            _assembly = assembly;
        }

        public Task<ScenarioInstance> Initialize()
        {
            _type = Type.GetType(_typeName + ", " + _assembly);

            _instance = Activator.CreateInstance(_type);
            return Task.FromResult(this);
        }

        public Task<ScenarioResult> Run()
        {
            foreach (var method in GetThenMethods())
                try
                {
                    method.RunSynchronously();
                    if (method.IsFaulted)
                    {
                        return ResultIsFault(method.Exception);
                    }
                }
                catch (Exception exception)
                {
                    return Task.FromResult(new ScenarioResult { Result = "fail" });
                }
            return Task.FromResult(new ScenarioResult() { Result = "success" });
        }

        Task<ScenarioResult> ResultIsFault(AggregateException exception)
        {
            return Task.FromResult(new ScenarioResult { Result = "fail" });
        }

        IEnumerable<Task> GetThenMethods()
        {
            return _type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                        .Where(IsTestMethod)
                        .Select(_ => new Task(() => _.Invoke(_instance, new object[0])));
        }

        static bool IsTestMethod(MethodInfo method)
        {
            return method.DeclaringType != typeof(object) && method.GetParameters().Length == 0;
        }

        public async Task<ScenarioInstance> Construct()

        {
            await Initialize();
            return this;
        }

        public void SetIntrospection(IDictionary<string, object> introspect)
        {
            var field = _type.GetField("scenario_details", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return;
            field.SetValue(_instance, introspect);
        }
    }

    public class ScenarioResult
    {
        public string Result;
    }
}

