using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using jacket.Reporting;
using Tests.Annotations;

namespace jacket
{
    public static class Program
    {
        static readonly Lazy<IEnumerable<Tuple<string, FileInfo>>> ASSEMBLY_FILES = 
            new Lazy<IEnumerable<Tuple<string, FileInfo>>>(()=> (GetAssemblyFiles()).ToList());

        static IEnumerable<Tuple<string, FileInfo>> GetAssemblyFiles()
        {
            return CURRENT_DIRECTORY.Value.GetFiles("*.dll").Select(file => Tuple.Create(file.Name, file));
        }

        static readonly Lazy<DirectoryInfo> CURRENT_DIRECTORY = new Lazy<DirectoryInfo>(() => new DirectoryInfo(Environment.CurrentDirectory));

        static IReporter CreateReporter(IEnumerable<string> args)
        {
            // temporary console parsing until we have the nice opencommandline stuff in
            if (args.Contains("-summary", StringComparer.OrdinalIgnoreCase))
                return new SummaryReporter();
            return new DetailsReporter();
        }

        static FileInfo GetDllUnderPath(string[] args)
        {
            return GetProvidedFileName(args)
                   ?? GetFileByConvention()
                   ?? GetAssemblyIfOnlyOne()
                   ?? NamedTests();
        }

        static FileInfo NamedTests()
        {
            var matching = ASSEMBLY_FILES.Value.Where(_ => _.Item1.IndexOf("Test", StringComparison.OrdinalIgnoreCase) != -1)
                                               .ToList();

            return matching.Count == 1 ? matching[0].Item2 : null;
        }

        static FileInfo GetAssemblyIfOnlyOne()
        {
            return (ASSEMBLY_FILES.Value.Count() == 1) ? ASSEMBLY_FILES.Value.First().Item2 : null;
        }

        static FileInfo GetFileByConvention()
        {
            return
                (
                    from directory in CURRENT_DIRECTORY.Value.SelfAndParents()
                    let selectedFile = ASSEMBLY_FILES.Value
                        .FirstOrDefault(_ => _.Item1.Equals(directory.Name, StringComparison.OrdinalIgnoreCase))
                    where selectedFile != null
                    select selectedFile.Item2
                ).FirstOrDefault();
        }

        static FileInfo GetProvidedFileName(string[] args)
        {
            if (args.Length > 0 && !args[0].StartsWith("-"))
            {
                var fi = new FileInfo(args[0]);
                if (fi.Exists) return fi;
            }
            return null;
        }

        static int Main(string[] args)
        {
            StartDebuggerWHenAttributePresent(args);

            var dllUnderTest = GetDllUnderPath(args);
            if (dllUnderTest == null)
                throw new InvalidOperationException("Cannot find an assembly to test.");
            Console.WriteLine("Testing {0} [{1}]", dllUnderTest.Name, dllUnderTest.FullName);
            var summaryReporter = CreateReporter(args);
            //Debugger.Break();
            var runComplete = MainAsync(summaryReporter, dllUnderTest);

            summaryReporter.RunUntilCompletion();
            Console.WriteLine("Finished.");
            WaitIfInteractive();
            return 0;
        }

        static void StartDebuggerWHenAttributePresent(string[] args)
        {
            if (args.Any(_ => _.Equals("-startdebug", StringComparison.OrdinalIgnoreCase)))
                Debugger.Launch();
        }

        static void WaitIfInteractive()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Press any key to terminate.");
                Console.ReadLine();
            }
        }

        static Task MainAsync(IReporter actionWriter, FileInfo assembly)
        {
            return new Story(assembly).Run(actionWriter.Success,
                                           actionWriter.Fail,
                                           actionWriter.Finished);
        }
    }

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
