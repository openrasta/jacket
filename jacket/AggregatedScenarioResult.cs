using System.Collections.Generic;
using System.Linq;

namespace jacket
{
    class AggregatedScenarioResult : ScenarioResult
    {
        public AggregatedScenarioResult(IEnumerable<ScenarioResult> thenResults)
        {
            var results = thenResults.ToList();
            Result = GetAggregatedResult(results);
            Metadata = GetAggregatedMetadata(results);
            
        }

        static IDictionary<string, object> GetAggregatedMetadata(IEnumerable<ScenarioResult> thenResults)
        {
            var returnMetadata = new Dictionary<string, object>();
            foreach (var kv in from result in thenResults
                               from entry in result.Metadata
                               select entry)
            {
                if (returnMetadata.ContainsKey(kv.Key) == false)
                    returnMetadata[kv.Key] = kv.Value;
            }
            return returnMetadata;
        }

        static string GetAggregatedResult(IEnumerable<ScenarioResult> thenResults)
        {
            return thenResults.Any(_ => _.Result != "success") ? "fail" : "success";
        }
    }
}