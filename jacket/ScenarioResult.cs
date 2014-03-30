using System.Collections.Generic;

namespace jacket
{
    public class ScenarioResult
    {
        public ScenarioResult(string result, IDictionary<string, object> metadata)
        {
            Result = result;
            Metadata = metadata;
        }

        protected ScenarioResult()
        {
            
        }

        public string Result { get; protected set; }
        public IDictionary<string,object> Metadata { get; protected set; }
        public const string SUCCESS = "success";
        public const string FAIL = "fail";
    }
}