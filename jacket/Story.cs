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
        FileInfo _assemblyFile;

        public Story(FileInfo assemblyFilePath)
        {
            _assemblyFile = assemblyFilePath;
            Scenarios = AssemblyDefinition.ReadAssembly(_assemblyFile.FullName)
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
                   && NotAttribute(typeDefinition)
                   && (typeDefinition.Name.Contains("_") || IsAllLower(typeDefinition.Name))
                   ;
        }

        bool IsAllLower(string name)
        {
            return name.Aggregate(true, (wasTrue, character) => wasTrue && char.IsLower(character));
        }

        bool NotAttribute(TypeDefinition typeDefinition)
        {
            return typeDefinition.SelfAndParents().Any(_ => _.FullName == typeof(Attribute).FullName) == false;
        }

        public IEnumerable<Scenario> Scenarios { get; set; }

        Scenario ToScenario(TypeDefinition typeDefinition)
        {
            return new Scenario(typeDefinition,_assemblyFile);
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