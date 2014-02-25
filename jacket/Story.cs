using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;

namespace jacket
{
    public class Story
    {
        public Story(string assemblyFilePath)
        {
            Scenarios = AssemblyDefinition.ReadAssembly(assemblyFilePath)
                                          .MainModule.Types
                                          .Where(IsScenario)
                                          .Select(ToScenario);
        }

        bool IsScenario(TypeDefinition typeDefinition)
        {
            return typeDefinition.IsAbstract == false
                   && typeDefinition.IsClass
                   && typeDefinition.IsEnum == false
                   && typeDefinition.IsPublic 
                   && !typeDefinition.HasGenericParameters
                   && typeDefinition.Name.Contains("_")
                   && NotAttribute(typeDefinition);
        }

        bool NotAttribute(TypeDefinition typeDefinition)
        {
            return typeDefinition.SelfAndParents().Any(_ => _.FullName == typeof(Attribute).FullName) == false;
        }

        public IEnumerable<Scenario> Scenarios { get; set; }

        Scenario ToScenario(TypeDefinition typeDefinition)
        {
            return new Scenario(typeDefinition, Path.GetFileNameWithoutExtension(typeDefinition.Module.Name));
        }

        public Task Run(Action<ScenarioResult> success, Action<ScenarioResult> fail, Action finished)
        {
            return Scenarios.Select(_ => _.RunAsync())
                            .On("success", success)
                            .On("fail", fail)
                            .Start(finished);
        }
    }
}