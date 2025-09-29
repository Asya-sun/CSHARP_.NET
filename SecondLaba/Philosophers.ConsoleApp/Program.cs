// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");



using Philosophers.ConsoleApp;
// For supporting Russian
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // For supporting Russian
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;


        var simulation = new Simulation();
        simulation.DurationMs = 300;

        simulation.Initialize("Naive");
        simulation.Run(displayStatsEveryMsec : 10);
    }
}