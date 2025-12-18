using Microsoft.Extensions.Logging;
using TableService.Interfaces;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TableService.Services
{
    public class MetricsCollectorService : IMetricsCollector
    {
        private readonly ILogger<MetricsCollectorService> _logger;
        private readonly ConcurrentDictionary<string, int> _eatCount = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _waitingTimes = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _thinkingTimes = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _eatingTimes = new();
        private readonly ConcurrentDictionary<int, Stopwatch> _forkUsageTimers = new();
        private readonly ConcurrentDictionary<int, TimeSpan> _forkTotalUsage = new();
        private readonly Stopwatch _simulationTimer = new Stopwatch();

        private int _deadlockCount = 0;

        public MetricsCollectorService(ILogger<MetricsCollectorService> logger)
        {
            _logger = logger;

            // Инициализация таймеров для вилок
            for (int i = 1; i <= 5; i++)
            {
                _forkUsageTimers[i] = new Stopwatch();
                _forkTotalUsage[i] = TimeSpan.Zero;
            }

            // Запускаем таймер симуляции
            _simulationTimer.Start();
        }

        public void RecordEating(string philosopherId)
        {
            _eatCount.AddOrUpdate(philosopherId, 1, (key, oldValue) => oldValue + 1);
        }

        public void RecordWaitingTime(string philosopherId, TimeSpan waitingTime)
        {
            var bag = _waitingTimes.GetOrAdd(philosopherId, new ConcurrentBag<TimeSpan>());
            bag.Add(waitingTime);
        }

        public void RecordThinkingTime(string philosopherId, TimeSpan thinkingTime)
        {
            var bag = _thinkingTimes.GetOrAdd(philosopherId, new ConcurrentBag<TimeSpan>());
            bag.Add(thinkingTime);
        }

        public void RecordEatingTime(string philosopherId, TimeSpan eatingTime)
        {
            var bag = _eatingTimes.GetOrAdd(philosopherId, new ConcurrentBag<TimeSpan>());
            bag.Add(eatingTime);
        }

        public void RecordDeadlock()
        {
            Interlocked.Increment(ref _deadlockCount);
            _logger.LogWarning("Зафиксирован дедлок #{DeadlockCount}", _deadlockCount);
        }

        public void RecordForkAcquired(int forkId, string philosopherId)
        {
            _forkUsageTimers[forkId].Restart();
        }

        public void RecordForkReleased(int forkId)
        {
            if (_forkUsageTimers[forkId].IsRunning)
            {
                _forkUsageTimers[forkId].Stop();
                var usageTime = _forkUsageTimers[forkId].Elapsed;
                _forkTotalUsage.AddOrUpdate(forkId, usageTime, (key, oldValue) => oldValue + usageTime);
            }
        }

        public int GetEatCount(string philosopherId)
        {
            return _eatCount.GetValueOrDefault(philosopherId, 0);
        }

        public void PrintMetrics()
        {
            var totalTime = GetTotalSimulationTime();
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                         МЕТРИКИ СИМУЛЯЦИИ                           ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            PrintThroughputMetrics(sb, totalTime);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintForkUtilizationMetrics(sb, totalTime);
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════╝");

            _logger.LogInformation("{Metrics}", sb.ToString());
        }

        private void PrintThroughputMetrics(StringBuilder sb, TimeSpan totalTime)
        {
            sb.AppendLine("║ ПРОПУСКНАЯ СПОСОБНОСТЬ (раз/сек):");
            int totalEatCount = 0;
            int philosopherCount = 0;

            foreach (var (philosopherId, count) in _eatCount)
            {
                double throughput = totalTime.TotalSeconds > 0 ? count / totalTime.TotalSeconds : 0;
                sb.AppendLine($"║   {philosopherId,-15}: {throughput,6:F3} раз/сек ({count,3} раз)");
                totalEatCount += count;
                philosopherCount++;
            }

            if (philosopherCount > 0)
            {
                double avgThroughput = totalTime.TotalSeconds > 0 ? totalEatCount / totalTime.TotalSeconds : 0;
                double avgPerPhilosopher = (double)totalEatCount / philosopherCount;
                sb.AppendLine("║");
                sb.AppendLine($"║   СРЕДНЯЯ: {avgThroughput,8:F3} раз/сек");
                sb.AppendLine($"║   СРЕДНЕЕ НА ФИЛОСОФА: {avgPerPhilosopher,5:F1} раз");
            }
        }


        private void PrintForkUtilizationMetrics(StringBuilder sb, TimeSpan totalTime)
        {
            sb.AppendLine("║ КОЭФФИЦИЕНТ УТИЛИЗАЦИИ ВИЛОК:");

            foreach (var (forkId, usageTime) in _forkTotalUsage.OrderBy(x => x.Key))
            {
                var utilization = totalTime.TotalMilliseconds > 0
                    ? (usageTime.TotalMilliseconds / totalTime.TotalMilliseconds) * 100
                    : 0;
                var freeTime = 100 - utilization;

                sb.AppendLine($"║   Вилка-{forkId}:");
                sb.AppendLine($"║     Использование: {utilization,6:F2}%");
                sb.AppendLine($"║     Свободна:     {freeTime,6:F2}%");
            }
        }

        public IReadOnlyDictionary<string, int> GetEatCounts()
        {
            return new Dictionary<string, int>(_eatCount);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetWaitingTimes()
        {
            return _waitingTimes.ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<TimeSpan>)x.Value.ToList()
            );
        }

        public IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetThinkingTimes()
        {
            return _thinkingTimes.ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<TimeSpan>)x.Value.ToList()
            );
        }

        public IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetEatingTimes()
        {
            return _eatingTimes.ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<TimeSpan>)x.Value.ToList()
            );
        }

        public IReadOnlyDictionary<int, TimeSpan> GetForkUsageTimes()
        {
            return new Dictionary<int, TimeSpan>(_forkTotalUsage);
        }

        public int GetDeadlockCount()
        {
            return _deadlockCount;
        }

        public TimeSpan GetTotalSimulationTime()
        {
            return _simulationTimer.Elapsed;
        }

        // Метод для остановки таймера при завершении симуляции
        public void StopSimulation()
        {
            _simulationTimer.Stop();
        }
    }
}