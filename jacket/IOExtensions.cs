using System;
using System.Collections.Generic;
using System.IO;
using Tests.Annotations;

namespace jacket
{
    public static class IOExtensions
    {
        public static IEnumerable<DirectoryInfo> SelfAndParents([NotNull] this DirectoryInfo origin)
        {
            if (origin == null) throw new ArgumentNullException("origin");
            var current = origin;
            do
            {
                yield return current;
                current = current.Parent;
            }
            while (current != null && current.Exists);
        }
    }
}