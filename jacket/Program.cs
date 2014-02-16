using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using jacket.Reporting;

namespace jacket
{
    public static class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Testing Tests.dll");
            IReporter summaryReporter = CreateReporter(args);
            var runComplete = MainAsync(summaryReporter, args);

            summaryReporter.RunUntilCompletion();
            Console.WriteLine("Finished.");

            return 0;
        }

        static IReporter CreateReporter(IEnumerable<string> args)
        {
            // temporary console parsing until we have the nice opencommandline stuff in
            if (args.Contains("summary", StringComparer.OrdinalIgnoreCase))
                return new SummaryReporter();
            return new DetailsReporter();
        }

        static Task MainAsync(IReporter actionWriter, string[] args)
        {
            return new Story("Tests.Dll").Run(
                actionWriter.Success,
                actionWriter.Fail,
                actionWriter.Finished);
        }
    }
}