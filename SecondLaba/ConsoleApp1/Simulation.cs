using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Philosophers.ConsoleApp
{
    public class Simulation
    {
        private List<Philosopher> _philosophers = new();
        private List<Fork> _forks = new();

        
        public bool _isRunning { get; private set; }

        public void Initialize(string strategyType)
        {
            CreateForks();
            CreatePhilosophers(strategyType);
            LoadPhilosopherNames();
        }

        private void CreateForks()
        {
            for (int i = 0; i < 5; i++)
            {
                _forks.Add(new Fork(i));

            }
        }

        private void CreatePhilosophers(string strategyType)
        {
            IPhilosopherStrategy strategy;
            // я хз, нужно тут было оставить фабрику или можно было забить, раз стратегия одна
            switch (strategyType)
            {
                case "Naive":
                    strategy = new NaiveStrategy();
                    break;
                default:
                    throw new ArgumentException($"Unknown strategy: {strategyName}")
            }

            for (int i = 0; i < 5; i++)
            {
                var philosopher = new Philosopher(i + 1, $"Философ-{i + 1}", _forks[i], _forks[(i + 1) % 5], strategy);


                philosopher._strategy.Initialize(philosopher);
                _philosophers.Add(philosopher);
            }
        }



        private void LoadPhilosopherNames()
        {
            //Console.WriteLine($"Текущая директория: {Directory.GetCurrentDirectory()}");
            //Console.WriteLine($"Существует ли файл: {File.Exists("names.json")}");
            try
            {

                string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?
                    .Parent?
                    .Parent?
                    .FullName ?? throw new InvalidOperationException("Не удалось определить путь к проекту");
                string filePath = Path.Combine(projectDir, "names.json");

                Console.WriteLine($"Ищем файл по пути: {filePath}");
                if (File.Exists(filePath))
                {
                    var names = JsonSerializer.Deserialize<string[]>(File.ReadAllText(filePath));

                    if (names != null)
                    {
                        for (int i = 0; i < Math.Min(names.Length, _philosophers.Count); i++)
                        {
                            _philosophers[i]._name = names[i];
                        }
                    }
                    else
                    {
                        Console.WriteLine("Файл names.json пуст или имеет неверный формат");
                    }
                }
            }
            catch
            {
                Console.WriteLine("Не удалось загрузить имена из файла, используем стандартные");
            }
        }

    }
}
