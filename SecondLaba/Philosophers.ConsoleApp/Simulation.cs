using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Strategies;
using System;
using System.Collections.Generic;
using System.Data;
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
        private CancellationTokenSource _cts = new();
        private Timer _statusTimer = null!;
        private DateTime _startTime;
        private long _totalSimulationTimeMs = 0;

        public bool _isRunning { get; private set; }
        // 10 секунд по умолчанию
        public int DurationMs { get; set; } = 10000; 

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
            for (int i = 0; i < 5; i++)
            {
                // я хз, нужно тут было оставить фабрику или можно было забить, раз стратегия одна
                IPhilosopherStrategy strategy = strategyType switch
                {
                    "Naive" => new NaiveStrategy(),
                    _ => throw new ArgumentException($"Unknown strategy: {strategyType}")
                };

                var philosopher = new Philosopher(i + 1, $"Философ-{i + 1}", _forks[i], _forks[(i + 1) % 5], strategy, _cts.Token );


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


        public void Run(int displayStatsEveryMsec = 200)
        {
            // тут вместо этого может быть исключение =)
            if (displayStatsEveryMsec <= 0)
            {
                displayStatsEveryMsec = 200;
                Console.WriteLine($"Предупреждение: Некорректный интервал отображения. Используется значение по умолчанию: {displayStatsEveryMsec} мс");
            }



            _startTime = DateTime.Now;
            _isRunning = true;

            // Создаем таймер, который вызывает метод UpdateStatus каждые displayStatsEveryMsec мс
            _statusTimer = new Timer(UpdateStatus, null, 0, displayStatsEveryMsec);

            // Запускаем всех философов
            foreach (var philosopher in _philosophers)
            {
                philosopher.Start();
            }

            //// тут создается Task (типа таймера), который по завершению вызывает эту функцию
            //Task.Delay(DurationMs).ContinueWith(_ => Stop());

            Thread.Sleep(DurationMs);
            Stop();
        }


        public void Stop()
        {
            _cts.Cancel();
            _isRunning = false;
            _statusTimer?.Dispose();
            _totalSimulationTimeMs = (long)(DateTime.Now - _startTime).TotalMilliseconds;

            foreach (var fork in _forks)
            {
                fork.UpdateMetrics();
            }

            foreach (var philosopher in _philosophers)
            {
                philosopher.Join();
            }

            DisplayMetrics();
        }

        private void UpdateStatus(object? state)
        {
            if (!_isRunning) return;

            //Console.Clear();
            Console.WriteLine($"===== Время: {(DateTime.Now - _startTime).TotalMilliseconds:0} мс =====");

            Console.WriteLine("Философы:");
            foreach (var philosopher in _philosophers)
            {
                // Убираем рандом и steps left -просто показываем состояние и действие
                Console.WriteLine($"  {philosopher._name}: {philosopher.State} (Action = {philosopher.CurrentAction}), съедено: {philosopher._mealsEaten}");
            }

            Console.WriteLine("\nВилки:");
            foreach (var fork in _forks)
            {
                string user = fork._currentUser != null ? $" (используется {fork._currentUser._name})" : "";
                Console.WriteLine($"  {fork._name}: {fork._state}{user}");
            }
        }


        private void DisplayMetrics()
        {
            Console.WriteLine("\n=== МЕТРИКИ СИМУЛЯЦИИ ===");
            Console.WriteLine($"Общее время симуляции: {_totalSimulationTimeMs} мс");

            // Пропускная способность
            int totalMeals = _philosophers.Sum(p => p._mealsEaten);
            double throughput = (double)totalMeals / _totalSimulationTimeMs * 1000; // еда/секунду
            Console.WriteLine($"\nПропускная способность: {throughput:0.0000} еды/с");
            Console.WriteLine($"Всего съедено: {totalMeals} раз");



            // По философам
            Console.WriteLine("\nФилософы:");
            foreach (var philosopher in _philosophers)
            {
                Console.WriteLine($"  {philosopher._name}: {philosopher._mealsEaten} meals, " +
                                $"Среднее время ожидания: {philosopher.GetAverageHungryTime():0.00} мс " +
                        $"Всего ждал: {philosopher.TotalHungryTimeMs} мс");
            }
            
            // Время ожидания
            var maxWait = _philosophers.MaxBy(p => p.GetAverageHungryTime());
        var avgWait = _philosophers.Average(p => p.GetAverageHungryTime());
            Console.WriteLine($"\nВремя ожидания:");
            Console.WriteLine($"  Среднее: {avgWait:0.00} мс");
            Console.WriteLine($"  Максимальное ({maxWait?._name}): {maxWait?.GetAverageHungryTime():0.00} мс");

            // Утилизация вилок
            Console.WriteLine("\nУтилизация вилок:");
            foreach (var fork in _forks)
            {
                double utilization = fork.GetUtilizationPercentage(_totalSimulationTimeMs);
                double availability = 100 - utilization;
                Console.WriteLine($"  {fork._name}: {utilization:0.00}% использования, {availability:0.00}% доступности");
                Console.WriteLine($"    Всего использована: {fork.TotalInUseTimeMs} мс, доступна: {fork.TotalAvailableTimeMs} мс");
            }

            
        }
    }
}
