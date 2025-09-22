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
        simulation.DurationMs = 10000; // 30 секунд

        Console.WriteLine("Запуск симуляции...");
        simulation.Initialize("Naive");
        simulation.Start(displayStatsEveryMsec : 300);
    }
}