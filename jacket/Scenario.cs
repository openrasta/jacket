using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

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

            _ctor = SelectConstructor();
            if (_ctor == null)
                throw new InvalidOperationException("No constructor found");

            _assembly = assemblyFilePath;

            _clrType = new Lazy<Type>(() => LoadTypeFromAssembly(_typeDefinition, assemblyFilePath));
            _exampleDataPoints = GetExamplesFieldValue();
            _introspect = GetIntrospectionDetails();
        }

        Type LoadTypeFromAssembly(TypeDefinition typeDefinition, FileInfo assemblyFilePath)
        {
            var asm = Assembly.LoadFrom(assemblyFilePath.FullName);
            return asm.GetType(typeDefinition.FullName);

        }

        public async Task<ScenarioResult> RunAsync()
        {
            if (_ctor.Parameters.Count == 0)
            {
                var instances = await Task.WhenAll(Construct(new Dictionary<string, object>()));

                var thenResults = await Task.WhenAll(instances.Select(_ => _.Run()));

                return AggregateResults(thenResults);
            }
            else
            {
                // ouch IEnumerable<IGrouping<IDictionary<string, object>, IEnumerable<Task<ScenarioInstance>>>>
                var examples = await Task.WhenAll(ConstructExamples(_exampleDataPoints));
                var resultsPerExample = await Task.WhenAll(examples.Select(_ => _.Run()));
                return AggregateResults(_introspect, resultsPerExample);
            }
        }

        ScenarioResult AggregateResults(IDictionary<string, object> template, ScenarioExample[] resultsPerExample)
        {
            return new ExamplesResult(template, resultsPerExample);
        }

        IEnumerable<Task<ScenarioExample>> ConstructExamples(IEnumerable<IDictionary<string, object>> examples)
        {
            return examples.Select(_ => new ScenarioExample(_, Construct(_)).Construct());
        }

        IEnumerable<IDictionary<string, object>> GetExamplesFieldValue()
        {
            var field = _clrType.Value.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(_ => _.Name == "examples" && _.FieldType == typeof(object[]));
            if (field == null) return Enumerable.Empty<IDictionary<string,object>>();
            var dataPoints = (object[])field.GetValue(null);
            return dataPoints.Select(ReadValues).ToList();
        }

        static Dictionary<string, object> ReadValues(object dataPoint)
        {
            return (dataPoint.GetType()
                             .GetProperties()
                             .Select(pi => new
                             {
                                 key = pi.Name,
                                 value = pi.GetValue(dataPoint)
                             }))
                             .ToDictionary(_ => _.key,_ => _.value);
        }

        ScenarioResult AggregateResults(IEnumerable<ScenarioResult> thenResults)
        {
            return new AggregatedScenarioResult(thenResults);
        }

        ICollection<LanguageElement> givens = new List<LanguageElement>();
        ICollection<LanguageElement> whens = new List<LanguageElement>();
        ICollection<LanguageElement> thens = new List<LanguageElement>();
        string displayName;
        readonly MethodDefinition _ctor;
        Lazy<Type> _clrType;
        IEnumerable<IDictionary<string, object>> _exampleDataPoints;

        IDictionary<string, object> GetIntrospectionDetails()
        {

            var methodCallsInConstructor = _ctor
                                             .Body.Instructions.Where(_ => _.OpCode == OpCodes.Call)
                                             .Select(_ => _.Operand).OfType<MethodReference>()
                                             .ToList();

            var introspectionDetails = new Dictionary<string, object>();
            AddLanguageElements(givens, "given", methodCallsInConstructor.Where(_ => _.Name.StartsWith("given")), introspectionDetails);
            AddLanguageElements(whens, "when", methodCallsInConstructor.Where(_ => _.Name.StartsWith("when")), introspectionDetails);
            _thenMethods = _typeDefinition.Methods.Where(IsThenMethod);
            AddLanguageElements(thens, "then", _thenMethods, introspectionDetails, false);

            introspectionDetails["display.name"] = displayName = _typeDefinition.Name.Replace("_", " ");
            return introspectionDetails;
        }

        MethodDefinition SelectConstructor()
        {
            return _typeDefinition.GetConstructors().FirstOrDefault();
        }

        bool IsThenMethod(MethodDefinition method)
        {
            return ThenMethodParametersInExample(method.Parameters)
                && method.DeclaringType == _typeDefinition
                && (method.DeclaringType.Interfaces.Any(_ => _.FullName == "System.IDisposable") == false
                    || method.Name != "Dispose")
                && method.IsConstructor == false;
        }

        bool ThenMethodParametersInExample(Collection<ParameterDefinition> parameters)
        {
            if (_exampleDataPoints.Any() == false) return parameters.Count == 0;
            var keys = _exampleDataPoints.SelectMany(_ => _.Keys);
            return parameters.Aggregate(true, (prev,param) => prev && keys.Contains(param.Name, StringComparer.OrdinalIgnoreCase));
        }

        void AddLanguageElements(ICollection<LanguageElement> destination, string prefix, IEnumerable<MethodReference> methods, IDictionary<string, object> introspectionDetails, bool codeHasPrefix = true)
        {
            var allKeys = new StringBuilder();
            foreach (var method in methods.Select(_ => AsLanguageElement(prefix, _, codeHasPrefix)))
            {
                allKeys.AppendIfNotEmpty(",").Append(method.Key);
                destination.Add(method);
                introspectionDetails.LanguageElement(method);
            }
            introspectionDetails.Add(prefix, allKeys.ToString());
        }

        static LanguageElement AsLanguageElement(string prefix, MethodReference method, bool codeHasPrefix = true)
        {

            var nameWithoutPrefix = codeHasPrefix && method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? method.Name.Substring(prefix.Length + 1)
                : method.Name;
            return new LanguageElement
                   {
                       MethodName = method.Name,
                       DisplayName = ApplyMarkdown(nameWithoutPrefix,method),
                       Key = nameWithoutPrefix,
                       Prefix = prefix
                   };
        }

        static string ApplyMarkdown(string name, MethodReference method)
        {
            var parameterNames = method.Parameters
                                       .Select(_ => new
                                       {
                                           param = _, 
                                           name = _.Name,
                                           underscore = CamelToUnderscore(_.Name)
                                       }).ToList();

            var highlights = parameterNames
                .Where(_=>name.IndexOf(_.name, StringComparison.OrdinalIgnoreCase) != -1 ||
                          name.IndexOf(_.underscore, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList();

            var missingParameters = parameterNames.Except(highlights);

            string nameWithHighlights = highlights
                .SelectMany(_=>new[]{_.name, _.underscore})
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Aggregate(name, (current, highlight) => 
                    current.Replace(highlight, highlight.Em()))
                .Replace("_", " ");

            string namedParameters = missingParameters.Select(_=>_.name.Em()).ConcatString(prefix: " for");
            return nameWithHighlights + namedParameters;
        }

        static string CamelToUnderscore(string argumentName)
        {
            var sb = new StringBuilder();
            foreach (var c in argumentName)
            {
                if (char.IsUpper(c))
                    sb.Append('_').Append(char.ToLower(c));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        IEnumerable<Task<ScenarioInstance>> Construct(IDictionary<string, object> ctorArgs)
        {
            return _thenMethods.Select(_ =>
                new ScenarioInstance(this, new Dictionary<string, object>(_introspect), _assembly, _typeDefinition.FullName, _ctor, ctorArgs, _clrType, _).Construct());
        }
    }

    class ExamplesResult : ScenarioResult
    {
        readonly IDictionary<string, object> _template;

        public ExamplesResult(IDictionary<string, object> template, ScenarioExample[] resultsPerExample)
        {
            _template = template;
            Result = resultsPerExample.Any(_ => _.Result.Result != SUCCESS) ? FAIL : SUCCESS;
            Metadata = AggregateMetadata(resultsPerExample);
            Examples = resultsPerExample;
        }

        public IEnumerable<ScenarioExample> Examples { get; set; }

        IDictionary<string, object> AggregateMetadata(IEnumerable<ScenarioExample> resultsPerExample)
        {
            return (from example in resultsPerExample.Select((result, pos) => new { result, pos })
                    let prefix = string.Format("example.{0}.", example.pos)
                    from kv in example.result.Result.Metadata
                    select new { Key = prefix + kv.Key, kv.Value }
                   )
                   .Concat(_template.Select(_=>new{_.Key,_.Value}))
                   .ToDictionary(_ => _.Key, _ => _.Value);

        }
    }

    class ScenarioExample
    {
        public IDictionary<string, object> Values { get; set; }
        readonly IEnumerable<Task<ScenarioInstance>> _instanceBuilders;
        ScenarioInstance[] _instances;
        ScenarioResult[] _results;

        public ScenarioExample(IDictionary<string, object> values, IEnumerable<Task<ScenarioInstance>> instanceBuilders)
        {
            Values = values;
            _instanceBuilders = instanceBuilders;
        }

        public async Task<ScenarioExample> Construct()
        {
            _instances = await Task.WhenAll(_instanceBuilders);
            return this;
        }

        public async Task<ScenarioExample> Run()
        {
            _results = await Task.WhenAll(_instances.Select(_ => _.Run()));
            Result = new AggregatedScenarioResult(_results);
            return this;
        }

        public AggregatedScenarioResult Result { get; set; }
    }
}

