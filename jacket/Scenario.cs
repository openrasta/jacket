using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        readonly FileInfo _assembly;
        IDictionary<string, object> _introspect;
        IEnumerable<MethodDefinition> _thenMethods;

        public Scenario(TypeDefinition typeDefinition, FileInfo assemblyFilePath)
        {
            _typeDefinition = typeDefinition;
            
            _introspect = GetIntrospectionDetails();
            _assembly = assemblyFilePath;
        }

        public async Task<ScenarioResult> RunAsync()
        {

            var instances = await Task.WhenAll(Construct());

            var thenResults = await Task.WhenAll(instances.Select(_ => _.Run()));

            return AggregateResults(thenResults);
        }

        ScenarioResult AggregateResults(IEnumerable<ScenarioResult> thenResults)
        {
            return new AggregatedScenarioResult(thenResults);
        }

        ICollection<LanguageElement> givens = new List<LanguageElement>();
        ICollection<LanguageElement> whens = new List<LanguageElement>();
        ICollection<LanguageElement> thens = new List<LanguageElement>();
        string displayName;

        IDictionary<string, object> GetIntrospectionDetails()
        {
            var methodCallsInConstructor = _typeDefinition.GetConstructors()
                                             .Single(_ => _.Parameters.Count == 0)
                                             .Body.Instructions.Where(_ => _.OpCode == OpCodes.Call)
                                             .Select(_=>_.Operand).OfType<MethodReference>()
                                             .ToList();

            var introspectionDetails = new Dictionary<string, object>();
            AddLanguageElements(givens,"given", methodCallsInConstructor.Where(_=>_.Name.StartsWith("given")), introspectionDetails);
            AddLanguageElements(whens, "when", methodCallsInConstructor.Where(_ => _.Name.StartsWith("when")), introspectionDetails);
            _thenMethods = _typeDefinition.Methods.Where(IsThenMethod);
            AddLanguageElements(thens, "then", _thenMethods, introspectionDetails, false);

            introspectionDetails["display.name"] = displayName = _typeDefinition.Name.Replace("_", " ");
            return introspectionDetails;
        }

        bool IsThenMethod(MethodDefinition method)
        {
            return method.Parameters.Count == 0
                && method.DeclaringType == _typeDefinition
                && (method.DeclaringType.Interfaces.Any(_=>_.FullName == "System.IDisposable") == false 
                    || method.Name != "Dispose")
                && method.IsConstructor == false;
        }

        void AddLanguageElements(ICollection<LanguageElement> destination, string prefix, IEnumerable<MethodReference> methods, IDictionary<string, object> introspectionDetails, bool codeHasPrefix = true)
        {
            var allKeys = new StringBuilder();
            foreach (var method in methods.Select(_=>AsLanguageElement(prefix, _, codeHasPrefix)))
            {
                allKeys.AppendIfNotEmpty(",").Append(method.Key);
                destination.Add(method);
                introspectionDetails.Add(string.Format("{0}.{1}.key",prefix, method.Key), method.Key);
                introspectionDetails.Add(string.Format("{0}.{1}.display.name", prefix, method.Key), method.DisplayName);
                introspectionDetails.Add(string.Format("{0}.{1}.method.name", prefix, method.Key), method.MethodName);
            }
            introspectionDetails.Add(prefix, allKeys.ToString());
        }

        static LanguageElement AsLanguageElement(string prefix, MethodReference method, bool codeHasPrefix = true)
        {

            var nameWithoutPrefix = codeHasPrefix && method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? method.Name.Substring(prefix.Length +1)
                : method.Name;
            return new LanguageElement
                   {
                       MethodName = method.Name,
                       DisplayName = nameWithoutPrefix.Replace("_", " "),
                       Key = nameWithoutPrefix
                   };
        }

        IEnumerable<Task<ScenarioInstance>> Construct()
        {
            return _thenMethods.Select(_ =>
                new ScenarioInstance(this, _typeDefinition.FullName, _assembly, _introspect, _).Construct());
        }
    }
}

