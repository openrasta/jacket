using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace jacket
{
    public static class ReflectionExtensions
    {
        public static void InvokeMatching(this IEnumerable<MethodBase> methods, object instance, IDictionary<string, object> arguments)
        {
            methods.GetActionMatchingArguments(instance, arguments)();
        }

        public static Action GetActionMatchingArguments(this IEnumerable<MethodBase> methods, object instance, IDictionary<string, object> arguments)
        {
            return (from constructor in
                        methods
                    let parameters = constructor.GetParameters()
                    let args = (from param in parameters
                                where arguments.ContainsKey(param.Name)
                                select arguments[param.Name]).ToArray()
                    where args.Length == parameters.Length
                    orderby args.Length descending
                    select (Action)(()=> constructor.Invoke(instance, args))
                   ).First();
        }
    }
}