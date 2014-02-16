using System.Collections.Generic;
using System.Linq;

namespace jacket
{
    class AggregatedScenarioResult : ScenarioResult
    {
        public AggregatedScenarioResult(IEnumerable<ScenarioResult> thenResults)
        {
            Result = thenResults.Any(_ => _.Result != "success") ? "fail" : "success";
        }
    }
}