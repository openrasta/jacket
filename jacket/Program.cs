using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jacket
{
    public static class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Testing Tests.dll");
            var summaryReporter = new SummaryReporter();
            var runComplete = MainAsync(summaryReporter, args);

            summaryReporter.RunUntilCompletion();
            Console.WriteLine("Finished.");

            return 0;
        }
        static Task MainAsync(SummaryReporter actionWriter, string[] args)
        {
            return new Story("Tests.Dll").Run(
                actionWriter.Success,
                actionWriter.Fail,
                actionWriter.Finished);
        }
    }
}