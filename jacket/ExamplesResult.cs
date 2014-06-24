using System.Collections.Generic;
using System.Linq;

namespace jacket
{
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
}