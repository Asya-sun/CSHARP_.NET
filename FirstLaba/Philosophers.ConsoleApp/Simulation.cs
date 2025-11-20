using Philosophers.Core.Interfaces;
using Philosophers.Core.Metrics;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Strategies;
using Philosophers.Strategies.Services;
using System.Text.Json;

// trouble - have to use absoulute path
// вынести количество философов в отдельную константу?

namespace Philosophers.Core
{
    public class Simulation
    {
        private List<Philosopher> _philosophers = new();
        private List<Fork> _forks = new();
        private int _totalSteps = 0;
        private readonly SimulationMetrics _metrics = new();
        private int _deadlockDetectedNumber = 0;
        // maybe would be better ???
        // private ICoordinator _coordinator = null!;
        private ICoordinator? _coordinator;
        private readonly IStrategyFactory _strategyFactory;
        private bool _inDeadlockNow = false;


        public Simulation(IStrategyFactory strategyFactory)
        {
            _strategyFactory = strategyFactory;
        }

        public void Initialize(string strategyType = "Naive")
        {
            CreateForks();
            CreatePhilosophers(strategyType);
            LoadPhilosopherNames();
        }

        private void CreateForks()
        {
            for (int i = 0; i < 5; i++)
            {
                _forks.Add(new Fork { Id = i, State = ForkState.Available });
            }
        }

        private void CreatePhilosophers(string strategyType)
        {


            if (strategyType == "Coordinated")
            {
                _coordinator = new Coordinator(_philosophers, _forks);
            }

            for (int i = 0; i < 5; i++)
            {
                var strategy = _strategyFactory.CreateStrategy(strategyType, _coordinator);
                var philosopher = new Philosopher
                {
                    _id = i,
                    // temporary
                    _name = $"Философ-{i + 1}",
                    _state = PhilosopherState.Thinking,
                    LeftFork = _forks[i],
                    RightFork = _forks[(i + 1) % 5],
                    Strategy = strategy
                };

                philosopher.Strategy.Initialize(philosopher);
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

        private void UpdateForkMetrics()
        {
            foreach (var fork in _forks)
            {
                if (!_metrics.ForkMetrics.ContainsKey(fork.Id))
                {
                    _metrics.ForkMetrics[fork.Id] = new ForkMetrics();
                }

                var metrics = _metrics.ForkMetrics[fork.Id];
                metrics.TotalSteps++;

                if (fork.State == ForkState.Available)
                    metrics.AvailableSteps++;
                else
                    metrics.InUseSteps++;
            }
        }

        

        public void Run(int maxSteps, int progressStep = 100000)
        {
            for (int step = 1; step <= maxSteps; step++)
            {
                _totalSteps = step;

                UpdateForkMetrics();

                foreach (var philosopher in _philosophers)
                {
                    philosopher.ExecuteStep(step);
                }

                // надо вообще?
                if (CheckForDeadlock())
                {
                    _deadlockDetectedNumber++;
                    Console.WriteLine($"DEADLOCK обнаружен на шаге {step}");
                    // Нужка ли логика восстановления?
                    _inDeadlockNow = true;
                    return;
                }

                if (step % progressStep == 0)
                {
                    PrintStep(step);
                }

            }

            return;
        }

        // лучше выносить в отдельную зависимость / интерфейс
        private bool CheckForDeadlock()
        {
            // Deadlock: все философы голодны и держат по одной вилке
            bool allHungry = _philosophers.All(p => p._state == PhilosopherState.Hungry);
            bool eachHasOneFork = _philosophers.All(p =>
                (p.HasLeftFork && !p.HasRightFork) || (!p.HasLeftFork && p.HasRightFork));

            return allHungry && eachHasOneFork;
        }

        private void PrintStep(int step)
        {
            Console.WriteLine($"\n===== ШАГ {step} =====");
            Console.WriteLine("Философы:");

            foreach (var philosopher in _philosophers)
            {
                string action = GetPhilosopherAction(philosopher);
                Console.WriteLine($"  {philosopher._name}: {philosopher._state} {action}, съедено: {philosopher._eatCount}");
            }

            Console.WriteLine("\nВилки:");
            foreach (var fork in _forks)
            {
                string user = fork.CurrentUserId.HasValue ?
                    _philosophers.First(p => p._id == fork.CurrentUserId)._name : "никем";
                Console.WriteLine($"  Вилка-{fork.Id + 1}: {fork.State} (используется {user})");
            }
        }

        private string GetPhilosopherAction(Philosopher philosopher)
        {
            if (philosopher._state != PhilosopherState.Hungry)
                return "";

            var actions = new List<string>();

            if (!philosopher.HasLeftFork && philosopher.CanTakeLeftFork() || philosopher.TakingLeftFork)
                actions.Add("TakeLeftFork");
            if (!philosopher.HasRightFork && philosopher.CanTakeRightFork() || philosopher.TakingRightFork)
                actions.Add("TakeRightFork");
            if (philosopher.HasLeftFork || philosopher.TakingLeftFork)
                actions.Add("ReleaseLeftFork");
            if (philosopher.HasRightFork || philosopher.TakingRightFork)
                actions.Add("ReleaseRightFork");

            return actions.Count > 0 ? $"(Action = {string.Join("|", actions)})" : "";
        }

        private void CalculateFinalMetrics()
        {
            _metrics.TotalSteps = _totalSteps;
            _metrics.TotalEatCount = _philosophers.Sum(p => p._eatCount);

            // Время голода
            _metrics.MaxHungerTime = _philosophers.Max(p => p.MaxHungryStreak);
            var maxHungerPhilosopher = _philosophers.FirstOrDefault(p => p.MaxHungryStreak == _metrics.MaxHungerTime);
            _metrics.MaxHungerPhilosopher = maxHungerPhilosopher?._name ?? "Unknown";
            // ???
            _metrics.AverageHungerTime = _philosophers.Sum(p => p._totalHungrySteps) / _metrics.TotalEatCount;
        }

        private void PrintMetrics()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("ФИНАЛЬНЫЕ РЕЗУЛЬТАТЫ СИМУЛЯЦИИ");
            Console.WriteLine(new string('=', 60));

            // Общая статистика
            Console.WriteLine($"\nОБЩАЯ СТАТИСТИКА:");
            Console.WriteLine($"Всего шагов симуляции: {_metrics.TotalSteps}");
            Console.WriteLine($"Обнаружено deadlock'ов: {_deadlockDetectedNumber}");
            Console.WriteLine($"Всего приемов пищи: {_metrics.TotalEatCount}");
            Console.WriteLine($"Общая пропускная способность: {_metrics.AverageThroughput:F2} еды/1000 шагов");

            // Статистика голода
            Console.WriteLine($"\nСТАТИСТИКА ВРЕМЕНИ ОЖИДАНИЯ:");
            Console.WriteLine($"Максимальное время голода: {_metrics.MaxHungerTime} шагов ({_metrics.MaxHungerPhilosopher})");
            Console.WriteLine($"Среднее время голода: {_metrics.AverageHungerTime:F2} шагов");

            // Детальная статистика по философам
            Console.WriteLine($"\nДЕТАЛЬНАЯ СТАТИСТИКА ПО ФИЛОСОФАМ:");
            foreach (var philosopher in _philosophers)
            {
                double throughput = (double)philosopher._eatCount / _metrics.TotalSteps * 1000;
                double hungerPercentage = _metrics.TotalSteps > 0 ?
                    (double)philosopher._totalHungrySteps / _metrics.TotalSteps * 100 : 0;

                Console.WriteLine($"{philosopher._name,-12}: " +
                    $"поел {philosopher._eatCount,3} раз, " +
                    $"{throughput,6:F2} еды/1000ш, " +
                    $"голод {philosopher._totalHungrySteps,4} шагов " +
                    $"({hungerPercentage,5:F1}%), " +
                    $"макс.голод {philosopher.MaxHungryStreak,3} шагов");
            }

            // Статистика по вилкам
            Console.WriteLine($"\nСТАТИСТИКА УТИЛИЗАЦИИ ВИЛОК:");
            foreach (var forkMetrics in _metrics.ForkMetrics.OrderBy(f => f.Key))
            {
                Console.WriteLine($"Вилка-{forkMetrics.Key + 1,-2}: " +
                    $"свободна {forkMetrics.Value.AvailabilityRate,5:F1}%, " +
                    $"используется {forkMetrics.Value.UtilizationRate,5:F1}%, " 
                    //$"заблокирована {100 - forkMetrics.Value.AvailabilityRate - forkMetrics.Value.UtilizationRate,5:F1}%"
                    );
            }

            // Итоговый Score
            Console.WriteLine($"\nИТОГОВЫЙ SCORE: {_metrics.TotalEatCount} единиц пищи");
            Console.WriteLine(new string('=', 60));
        }

        public void PrintResults()
        {
            if (_inDeadlockNow)
            {
                Console.WriteLine($"DEADLOCK now; do you really need metrics?");
                return;
            }
            CalculateFinalMetrics();
            PrintMetrics();
        }
    }
}