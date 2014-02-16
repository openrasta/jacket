using System.Collections.Generic;

namespace jacket
{
    public static class ResultExtensions
    {
        public static IEnumerable<string> GivenKeys(this IDictionary<string, object> metadata)
        {
            return ((string)metadata["given"]).SplitString(',');
        }

        public static IEnumerable<string> WhenKeys(this IDictionary<string, object> metadata)
        {
            return ((string)metadata["when"]).SplitString(',');
        }

        public static IEnumerable<string> ThenKeys(this IDictionary<string, object> metadata)
        {
            return ((string)metadata["then"]).SplitString(',');
        }
        public static string DisplayName(this IDictionary<string, object> metadata, string prefix, string key)
        {
            return ((string)metadata[string.Format("{0}.{1}.display.name", prefix, key)]);
        }
    }
}