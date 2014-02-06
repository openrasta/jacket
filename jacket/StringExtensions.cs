using System.Text;

namespace jacket
{
    public static class StringExtensions
    {
        public static StringBuilder AppendIfNotEmpty(this StringBuilder builder, string value)
        {
            return builder.Length == 0 ? builder : builder.Append(value);
        }
    }
}