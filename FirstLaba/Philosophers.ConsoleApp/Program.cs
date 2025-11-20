using Philosophers.Core;
using Philosophers.Strategies;

// For supporting Russian
using System.Text;

namespace Philosophers.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // For supporting Russian
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("Запуск симуляции обедающих философов...");

            var strategyFactory = new StrategyFactory();
            var simulation = new Simulation(strategyFactory);
            simulation.Initialize("Coordinated");
            //simulation.Initialize("Naive");
            simulation.Run(100000, 1000);
            simulation.PrintResults();
        }
    }
}