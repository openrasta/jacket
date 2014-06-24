using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jacket
{
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