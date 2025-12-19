using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Shared.DTO;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using TableService.Interfaces;
using TableService.Models;

namespace TableService.Services
{
    public class TableMetricsCollectorService : ITableMetricsCollector
    {
        private readonly ILogger<TableMetricsCollectorService> _logger;
        private readonly ConcurrentDictionary<string, int> _eatCount = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _waitingTimes = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _thinkingTimes = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _eatingTimes = new();
        private readonly ConcurrentDictionary<int, Stopwatch> _forkUsageTimers = new();
        private readonly ConcurrentDictionary<int, TimeSpan> _forkTotalUsage = new();
        private readonly Stopwatch _simulationTimer = new();

        // Новые поля для метрик от философов
        private readonly ConcurrentDictionary<string, UnregisterPhilosopherRequest> _philosopherMetrics = new();
        private int _deadlockCount = 0;

        public TableMetricsCollectorService(ILogger<TableMetricsCollectorService> logger, IOptions<TableConfig> config)
        {
            _logger = logger;
            _simulationTimer.Start();

            int forkCount = config.Value.PhilosophersCount == 1 ? 2 : config.Value.PhilosophersCount;

            for (int i = 1; i <= forkCount; i++)
            {
                _forkUsageTimers[i] = new Stopwatch();
                _forkTotalUsage[i] = TimeSpan.Zero;
            }
        }

        // Существующие методы
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

        public int GetDeadlockCount()
        {
            return _deadlockCount;
        }

        // Новый метод для записи метрик от философов
        public void RecordPhilosopherMetrics(UnregisterPhilosopherRequest philosopherMetrics)
        {
            // Сохраняем полные метрики
            _philosopherMetrics.AddOrUpdate(
                philosopherMetrics.PhilosopherId,
                philosopherMetrics,
                (key, oldValue) => philosopherMetrics);

            // Также сохраняем детальные данные в старые коллекции для совместимости
            if (!_eatCount.ContainsKey(philosopherMetrics.PhilosopherId))
            {
                _eatCount[philosopherMetrics.PhilosopherId] = philosopherMetrics.EatCount;
            }

            _logger.LogInformation(
                "Метрики получены от философа {PhilosopherId}: {EatCount} приемов пищи, " +
                "Голод: avg={AvgHungry}, total={TotalHungry}, max={MaxHungry}",
                philosopherMetrics.PhilosopherId,
                philosopherMetrics.EatCount,
                philosopherMetrics.AverageHungryTime,
                philosopherMetrics.TotalHungryTime,
                philosopherMetrics.MaximumHungryTime);
        }

        //public void PrintMetrics()
        //{
        //    var totalTime = GetTotalSimulationTime();
        //    var sb = new StringBuilder();

        //    sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
        //    sb.AppendLine("║                              СВОДКА МЕТРИК СИМУЛЯЦИИ                               ║");
        //    sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════════════╣");

        //    // Основная статистика
        //    sb.AppendLine($"║ Общее время симуляции: {totalTime:hh\\:mm\\:ss\\.fff}");
        //    sb.AppendLine($"║ Количество дедлоков: {_deadlockCount}");
        //    sb.AppendLine($"║ Количество философов: {_philosopherMetrics.Count}");
        //    sb.AppendLine("╟──────────────────────────────────────────────────────────────────────────────────────╢");

        //    // Статистика философов (из полученных метрик)
        //    PrintPhilosopherMetrics(sb);
        //    sb.AppendLine("╟──────────────────────────────────────────────────────────────────────────────────────╢");

        //    // Статистика вилок
        //    PrintForkUtilizationMetrics(sb, totalTime);
        //    sb.AppendLine("╟──────────────────────────────────────────────────────────────────────────────────────╢");

        //    // Сводная статистика
        //    PrintSummaryMetrics(sb);
        //    sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");

        //    _logger.LogInformation("{Metrics}", sb.ToString());
        //}

        //private void PrintPhilosopherMetrics(StringBuilder sb)
        //{
        //    sb.AppendLine("║ МЕТРИКИ ФИЛОСОФОВ:");

        //    if (_philosopherMetrics.IsEmpty)
        //    {
        //        sb.AppendLine("║   (метрики еще не получены)");
        //        return;
        //    }

        //    foreach (var (philosopherId, metrics) in _philosopherMetrics.OrderBy(x => x.Key))
        //    {
        //        sb.AppendLine($"║   {philosopherId}:");
        //        sb.AppendLine($"║     Приемов пищи: {metrics.EatCount}");
        //        sb.AppendLine($"║     Среднее голодание: {metrics.AverageHungryTime:mm\\:ss\\.fff}");
        //        sb.AppendLine($"║     Средняя еда: {metrics.AverageEatingTime:mm\\:ss\\.fff}");
        //        sb.AppendLine($"║     Среднее мышление: {metrics.AverageThinkingTime:mm\\:ss\\.fff}");
        //        sb.AppendLine($"║     Макс. голодание: {metrics.MaximumHungryTime:mm\\:ss\\.fff}");
        //    }
        //}

        //private void PrintForkUtilizationMetrics(StringBuilder sb, TimeSpan totalTime)
        //{
        //    sb.AppendLine("║ УТИЛИЗАЦИЯ ВИЛОК:");

        //    foreach (var (forkId, usageTime) in _forkTotalUsage.OrderBy(x => x.Key))
        //    {
        //        var utilization = totalTime.TotalMilliseconds > 0
        //            ? (usageTime.TotalMilliseconds / totalTime.TotalMilliseconds) * 100
        //            : 0;
        //        var freeTime = 100 - utilization;

        //        sb.AppendLine($"║   Вилка-{forkId}: {utilization,6:F2}% ({usageTime:mm\\:ss\\.fff})");
        //    }
        //}

        //private void PrintSummaryMetrics(StringBuilder sb)
        //{
        //    sb.AppendLine("║ СВОДНАЯ СТАТИСТИКА:");

        //    if (_philosopherMetrics.IsEmpty)
        //    {
        //        sb.AppendLine("║   (нет данных)");
        //        return;
        //    }

        //    int totalEatCount = _philosopherMetrics.Values.Sum(m => m.EatCount);
        //    var totalTime = GetTotalSimulationTime();
        //    double throughput = totalTime.TotalSeconds > 0 ? totalEatCount / totalTime.TotalSeconds : 0;

        //    // Средние значения по всем философам
        //    var avgHungryTime = TimeSpan.FromTicks(
        //        (long)_philosopherMetrics.Values.Average(m => m.AverageHungryTime.Ticks));
        //    var avgEatingTime = TimeSpan.FromTicks(
        //        (long)_philosopherMetrics.Values.Average(m => m.AverageEatingTime.Ticks));
        //    var avgThinkingTime = TimeSpan.FromTicks(
        //        (long)_philosopherMetrics.Values.Average(m => m.AverageThinkingTime.Ticks));

        //    sb.AppendLine($"║   Всего приемов пищи: {totalEatCount}");
        //    sb.AppendLine($"║   Пропускная способность: {throughput:F3} раз/сек");
        //    sb.AppendLine($"║   Среднее голодание (по философам): {avgHungryTime:mm\\:ss\\.fff}");
        //    sb.AppendLine($"║   Средняя еда (по философам): {avgEatingTime:mm\\:ss\\.fff}");
        //    sb.AppendLine($"║   Среднее мышление (по философам): {avgThinkingTime:mm\\:ss\\.fff}");
        //}

        public void PrintMetrics()
        {
            var totalTime = GetTotalSimulationTime();
            var sb = new StringBuilder();

            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                         МЕТРИКИ СИМУЛЯЦИИ                           ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

            PrintThroughputMetrics(sb, totalTime);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintWaitingTimeMetrics(sb);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintEatingTimeMetrics(sb);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintThinkingTimeMetrics(sb);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintForkUtilizationMetrics(sb, totalTime);
            sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
            PrintDeadlockMetrics(sb);
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════╝");

            _logger.LogInformation("{Metrics}", sb.ToString());
        }

        private void PrintDeadlockMetrics(StringBuilder sb)
        {
            sb.AppendLine("║ ДЕДЛОКИ:");
            if (_deadlockCount > 0)
            {
                sb.AppendLine($"║   Обнаружено дедлоков: {_deadlockCount}");
            }
            else
            {
                sb.AppendLine($"║   Дедлоков не обнаружено");
            }
        }

        private void PrintThroughputMetrics(StringBuilder sb, TimeSpan totalTime)
        {
            sb.AppendLine("║ ПРОПУСКНАЯ СПОСОБНОСТЬ (раз/сек):");

            if (_philosopherMetrics.IsEmpty)
            {
                sb.AppendLine("║   (данные не получены)");
                return;
            }

            int totalEatCount = 0;
            int philosopherCount = 0;

            // Пропускная способность по каждому философу
            foreach (var (philosopherId, metrics) in _philosopherMetrics.OrderBy(x => x.Key))
            {
                double throughput = totalTime.TotalSeconds > 0
                    ? metrics.EatCount / totalTime.TotalSeconds
                    : 0;
                sb.AppendLine($"║   {philosopherId,-15}: {throughput,6:F3} раз/сек ({metrics.EatCount,3} раз)");
                totalEatCount += metrics.EatCount;
                philosopherCount++;
            }

            if (philosopherCount > 0)
            {
                double avgThroughput = totalTime.TotalSeconds > 0
                    ? totalEatCount / totalTime.TotalSeconds
                    : 0;
                double avgPerPhilosopher = (double)totalEatCount / philosopherCount;
                sb.AppendLine("║");
                sb.AppendLine($"║   СРЕДНЯЯ: {avgThroughput,8:F3} раз/сек");
                sb.AppendLine($"║   СРЕДНЕЕ НА ФИЛОСОФА: {avgPerPhilosopher,5:F1} раз");
            }
        }

        private void PrintWaitingTimeMetrics(StringBuilder sb)
        {
            sb.AppendLine("║ ВРЕМЯ ГОЛОДАНИЯ (Hungry state):");

            if (_philosopherMetrics.IsEmpty)
            {
                sb.AppendLine("║   (данные не получены)");
                return;
            }

            TimeSpan maxWaitingTime = TimeSpan.Zero;
            string? maxWaitingPhilosopher = null;
            double totalAverageWaitingMs = 0;
            int philosophersWithWaiting = 0;

            foreach (var (philosopherId, metrics) in _philosopherMetrics.OrderBy(x => x.Key))
            {
                var average = metrics.AverageHungryTime;
                var max = metrics.MaximumHungryTime;

                sb.AppendLine($"║   {philosopherId,-15}: ср. {average.TotalMilliseconds,6:F0} мс, макс {max.TotalMilliseconds,6:F0} мс");

                totalAverageWaitingMs += average.TotalMilliseconds;
                philosophersWithWaiting++;

                if (max > maxWaitingTime)
                {
                    maxWaitingTime = max;
                    maxWaitingPhilosopher = philosopherId;
                }
            }

            if (philosophersWithWaiting > 0)
            {
                double overallAverage = totalAverageWaitingMs / philosophersWithWaiting;
                sb.AppendLine("║");
                sb.AppendLine($"║   СРЕДНЕЕ ПО ВСЕМ: {overallAverage,8:F0} мс");
                sb.AppendLine($"║   МАКСИМАЛЬНОЕ: {maxWaitingTime.TotalMilliseconds,8:F0} мс ({maxWaitingPhilosopher})");
            }
        }

        private void PrintEatingTimeMetrics(StringBuilder sb)
        {
            sb.AppendLine("║ ВРЕМЯ ПРИЕМА ПИЩИ (Eating state):");

            if (_philosopherMetrics.IsEmpty)
            {
                sb.AppendLine("║   (данные не получены)");
                return;
            }

            TimeSpan maxEatingTime = TimeSpan.Zero;
            string? maxEatingPhilosopher = null;
            double totalAverageEatingMs = 0;
            int philosophersCount = 0;

            foreach (var (philosopherId, metrics) in _philosopherMetrics.OrderBy(x => x.Key))
            {
                var average = metrics.AverageEatingTime;
                var max = metrics.MaximumEatingTime;

                sb.AppendLine($"║   {philosopherId,-15}: ср. {average.TotalMilliseconds,6:F0} мс, макс {max.TotalMilliseconds,6:F0} мс");

                totalAverageEatingMs += average.TotalMilliseconds;
                philosophersCount++;

                if (max > maxEatingTime)
                {
                    maxEatingTime = max;
                    maxEatingPhilosopher = philosopherId;
                }
            }

            if (philosophersCount > 0)
            {
                double overallAverage = totalAverageEatingMs / philosophersCount;
                sb.AppendLine("║");
                sb.AppendLine($"║   СРЕДНЕЕ ПО ВСЕМ: {overallAverage,8:F0} мс");
                sb.AppendLine($"║   МАКСИМАЛЬНОЕ: {maxEatingTime.TotalMilliseconds,8:F0} мс ({maxEatingPhilosopher})");
            }
        }

        private void PrintThinkingTimeMetrics(StringBuilder sb)
        {
            sb.AppendLine("║ ВРЕМЯ МЫШЛЕНИЯ (Thinking state):");

            if (_philosopherMetrics.IsEmpty)
            {
                sb.AppendLine("║   (данные не получены)");
                return;
            }

            TimeSpan maxThinkingTime = TimeSpan.Zero;
            string? maxThinkingPhilosopher = null;
            double totalAverageThinkingMs = 0;
            int philosophersCount = 0;

            foreach (var (philosopherId, metrics) in _philosopherMetrics.OrderBy(x => x.Key))
            {
                var average = metrics.AverageThinkingTime;
                var max = metrics.MaximumThinkingTime;

                sb.AppendLine($"║   {philosopherId,-15}: ср. {average.TotalMilliseconds,6:F0} мс, макс {max.TotalMilliseconds,6:F0} мс");

                totalAverageThinkingMs += average.TotalMilliseconds;
                philosophersCount++;

                if (max > maxThinkingTime)
                {
                    maxThinkingTime = max;
                    maxThinkingPhilosopher = philosopherId;
                }
            }

            if (philosophersCount > 0)
            {
                double overallAverage = totalAverageThinkingMs / philosophersCount;
                sb.AppendLine("║");
                sb.AppendLine($"║   СРЕДНЕЕ ПО ВСЕМ: {overallAverage,8:F0} мс");
                sb.AppendLine($"║   МАКСИМАЛЬНОЕ: {maxThinkingTime.TotalMilliseconds,8:F0} мс ({maxThinkingPhilosopher})");
            }
        }

        private void PrintForkUtilizationMetrics(StringBuilder sb, TimeSpan totalTime)
        {
            sb.AppendLine("║ КОЭФФИЦИЕНТ УТИЛИЗАЦИИ ВИЛОК:");

            if (_forkTotalUsage.IsEmpty)
            {
                sb.AppendLine("║   (данные не получены)");
                return;
            }

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

        // Методы для тестов и получения данных
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

        public IReadOnlyDictionary<string, UnregisterPhilosopherRequest> GetAllPhilosopherMetrics()
        {
            return new Dictionary<string, UnregisterPhilosopherRequest>(_philosopherMetrics);
        }

        public TimeSpan GetTotalSimulationTime()
        {
            return _simulationTimer.Elapsed;
        }

        public void StopSimulation()
        {
            _simulationTimer.Stop();
            _logger.LogInformation("Сбор метрик остановлен. Общее время: {TotalTime}", GetTotalSimulationTime());
        }
    }
}