using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace jacket
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Actions = new ConcurrentBag<Action>();
            Console.WriteLine("Testing Tests.dll");

            MainAsync(args).Wait();
            foreach (var action in Actions)
                action();
            Console.WriteLine("Finished.");
            Console.ReadLine();
        }
        static async Task<int> MainAsync(string[] args)
        {
            await new Story("Tests.Dll").Run(
                success => Actions.Add(()=>Console.WriteLine(".")),
                fail=>Actions.Add(()=>Console.WriteLine('X')));
            return 0;
        }

        static ConcurrentBag<Action> Actions { get; set; }

    }
}