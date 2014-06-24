using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace jacket
{
    public static class StringExtensions
    {
        public static StringBuilder AppendIfNotEmpty(this StringBuilder builder, string value)
        {
            return builder.Length == 0 ? builder : builder.Append(value);
        }

        public static IEnumerable<string> SplitString(this string value, params char[] separator)
        {
            return value.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(_=>_.Trim());
        }

        public static string ConcatString(this IEnumerable<string> values, string separator = " ", string prefix = "")
        {
            return values.Any()
                       ? values.Aggregate(prefix, (previous, item) => previous + separator + item)
                       : string.Empty;
        }

        public static string Em(this string input)
        {
            return "*" + input + "*";
        }
        public static string Capitalize(this string value)
        {
            return char.ToUpper(value[0]) + value.Substring(1);
        }
    }
}