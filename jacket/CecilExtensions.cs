using System.Collections.Generic;
using Mono.Cecil;

namespace jacket
{
    public static class CecilExtensions
    {
        public static IEnumerable<TypeReference> SelfAndParents(this TypeReference typeReference)
        {
            while (typeReference != null)
            {
                yield return typeReference;
                typeReference = typeReference.Resolve().BaseType;
            }
        }
    }
}