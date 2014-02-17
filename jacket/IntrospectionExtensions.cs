﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace jacket
{
    public static class IntrospectionExtensions
    {
        public static IEnumerable<string> GivenKeys(this IDictionary<string, object> metadata)
        {
            return metadata.Keys("given");
        }

        static IEnumerable<string> Keys(this IDictionary<string, object> metadata, string prefix)
        {
            return ((string)metadata[prefix]).SplitString(',');
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
        public static string MethodName(this IDictionary<string, object> metadata, string prefix, string key)
        {
            return ((string)metadata[string.Format("{0}.{1}.method.name", prefix, key)]);
        }

        public static string Result(this IDictionary<string, object> metadata, string prefix, string key)
        {
            return ((string)metadata[string.Format("{0}.{1}.result", prefix, key)]);
        }

        public static IEnumerable<Tuple<string, string, string>> MethodNames(this IDictionary<string, object> metadata)
        {
            return metadata.GivenKeys().Select(_=>Tuple.Create("given",_))
                           .Concat(metadata.WhenKeys().Select(_=>Tuple.Create("when",_)))
                           .Concat(metadata.ThenKeys().Select(_=>Tuple.Create("then",_)))
                           .Select(_=>Tuple.Create(_.Item1, _.Item2, metadata.MethodName(_.Item1, _.Item2)));
        } 
    }
}