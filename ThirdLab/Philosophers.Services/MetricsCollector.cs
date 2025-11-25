using Philosophers.Core.Interfaces;
using Philosophers.Core;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Collections.Concurrent;
using Philosophers.Core.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Philosophers.Services;

public class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<PhilosopherName, int> _eatCount = new();
    private readonly ConcurrentDictionary<PhilosopherName, ConcurrentBag<TimeSpan>> _waitingTimes = new();
    private readonly ConcurrentDictionary<PhilosopherName, ConcurrentBag<TimeSpan>> _thinkingTimes = new();
    private readonly ConcurrentDictionary<PhilosopherName, ConcurrentBag<TimeSpan>> _eatingTimes = new();
    private readonly ConcurrentDictionary<int, Stopwatch> _forkUsageTimers = new();
    private readonly ConcurrentDictionary<int, TimeSpan> _forkTotalUsage = new();
    private readonly SimulationOptions _options;

    private int _deadlockCount = 0;
    public MetricsCollector(ILogger<MetricsCollector> logger, IOptions<SimulationOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Инициализация таймеров для вилок
        for (int i = 1; i <= 5; i++)
        {
            _forkUsageTimers[i] = new Stopwatch();
            _forkTotalUsage[i] = TimeSpan.Zero;
        }
    }

    public void RecordEating(PhilosopherName philosopherName)
    {
        _eatCount.AddOrUpdate(philosopherName, 1, (key, oldValue) => oldValue + 1);
    }

    public void RecordWaitingTime(PhilosopherName philosopherName, TimeSpan waitingTime)
    {
        var bag = _waitingTimes.GetOrAdd(philosopherName, new ConcurrentBag<TimeSpan>());
        bag.Add(waitingTime);
    }

    public void RecordThinkingTime(PhilosopherName philosopherName, TimeSpan thinkingTime)
    {
        var bag = _thinkingTimes.GetOrAdd(philosopherName, new ConcurrentBag<TimeSpan>());
        bag.Add(thinkingTime);
    }

    public void RecordEatingTime(PhilosopherName philosopherName, TimeSpan eatingTime)
    {
        var bag = _eatingTimes.GetOrAdd(philosopherName, new ConcurrentBag<TimeSpan>());
        bag.Add(eatingTime);
    }

    public void RecordDeadlock()
    {
        // Увеличиваем счетчик дедлоков
        Interlocked.Increment(ref _deadlockCount);

        // Можно также логировать или сохранять время последнего дедлока
        _logger.LogWarning("Зафиксирован дедлок #{DeadlockCount}", _deadlockCount);
    }

    
    public void RecordForkAcquired(int forkId, PhilosopherName philosopherName)
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

    public int GetEatCount(PhilosopherName philosopherName)
    {
        return _eatCount.GetValueOrDefault(philosopherName, 0);
    }

    public void PrintMetrics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                         МЕТРИКИ СИМУЛЯЦИИ                           ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════════════╣");

        PrintThroughputMetrics(sb);
        sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
        PrintWaitingTimeMetrics(sb);
        sb.AppendLine("╟──────────────────────────────────────────────────────────────────────╢");
        PrintForkUtilizationMetrics(sb);
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
            double deadlocksPerMinute = _deadlockCount / (_options.DurationSeconds / 60.0);
            sb.AppendLine($"║   Частота: {deadlocksPerMinute:F2} дедлоков/минуту");
        }
        else
        {
            sb.AppendLine($"║   Дедлоков не обнаружено");
        }
    }

    private void PrintThroughputMetrics(StringBuilder sb)
    {
        sb.AppendLine("║ ПРОПУСКНАЯ СПОСОБНОСТЬ (раз/сек):");
        int totalEatCount = 0;
        int philosopherCount = 0;

        // Пропускная способность по каждому философу
        foreach (var (philosopher, count) in _eatCount)
        {
            double throughput = count / _options.DurationSeconds;
            sb.AppendLine($"║   {PhilosopherExtensions.ToName(philosopher),-12}: {throughput,6:F3} раз/сек ({count,3} раз)");
            totalEatCount += count;
            philosopherCount++;
        }

        if (philosopherCount > 0)
        {
            double avgThroughput = totalEatCount / _options.DurationSeconds;
            double avgPerPhilosopher = (double)totalEatCount / philosopherCount;
            sb.AppendLine("║");
            sb.AppendLine($"║   СРЕДНЯЯ: {avgThroughput,8:F3} раз/сек");
            sb.AppendLine($"║   СРЕДНЕЕ НА ФИЛОСОФА: {avgPerPhilosopher,5:F1} раз");
        }
    }

    private void PrintWaitingTimeMetrics(StringBuilder sb)
    {
        sb.AppendLine("║ ВРЕМЯ ОЖИДАНИЯ (Hungry state):");

        TimeSpan maxWaitingTime = TimeSpan.Zero;
        PhilosopherName? maxWaitingPhilosopher = null;
        double totalAverageWaiting = 0;
        int philosophersWithWaiting = 0;

        foreach (var philosopher in _eatCount.Keys)
        {
            if (_waitingTimes.ContainsKey(philosopher) && _waitingTimes[philosopher].Any())
            {
                var waitingTimes = _waitingTimes[philosopher].ToList();
                var average = TimeSpan.FromMilliseconds(waitingTimes.Average(t => t.TotalMilliseconds));
                var max = waitingTimes.Max();

                sb.AppendLine($"║   {PhilosopherExtensions.ToName(philosopher),-12}: ср. {average.TotalMilliseconds,6:F0} мс, макс {max.TotalMilliseconds,6:F0} мс");

                totalAverageWaiting += average.TotalMilliseconds;
                philosophersWithWaiting++;

                if (max > maxWaitingTime)
                {
                    maxWaitingTime = max;
                    maxWaitingPhilosopher = philosopher;
                }
            }
            else
            {
                sb.AppendLine($"║   {philosopher,-12}: не было периодов ожидания");
            }
        }

        if (philosophersWithWaiting > 0)
        {
            double overallAverage = totalAverageWaiting / philosophersWithWaiting;
            String maxWaitingPhilosopherString = maxWaitingPhilosopher.HasValue ? PhilosopherExtensions.ToName(maxWaitingPhilosopher.Value) : "unknown";
            sb.AppendLine("║");
            sb.AppendLine($"║   СРЕДНЕЕ ПО ВСЕМ: {overallAverage,8:F0} мс");
            sb.AppendLine($"║   МАКСИМАЛЬНОЕ: {maxWaitingTime.TotalMilliseconds,8:F0} мс ( {maxWaitingPhilosopherString})");
        }
    }

    private void PrintForkUtilizationMetrics(StringBuilder sb)
    {
        sb.AppendLine("║ КОЭФФИЦИЕНТ УТИЛИЗАЦИИ ВИЛОК:");
        var totalSimulationTime = TimeSpan.FromSeconds(_options.DurationSeconds);

        foreach (var (forkId, usageTime) in _forkTotalUsage.OrderBy(x => x.Key))
        {
            var utilization = (usageTime.TotalMilliseconds / totalSimulationTime.TotalMilliseconds) * 100;
            var freeTime = 100 - utilization;

            sb.AppendLine($"║   Вилка-{forkId}:");
            sb.AppendLine($"║     Использование: {utilization,6:F2}%");
            sb.AppendLine($"║     Свободна:     {freeTime,6:F2}%");
        }
    }

    public IReadOnlyDictionary<PhilosopherName, int> GetEatCounts()
    {
        return new Dictionary<PhilosopherName, int>(_eatCount);
    }

    public IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetWaitingTimes()
    {
        return _waitingTimes.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<TimeSpan>)x.Value.ToList()
        );
    }

    public IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetThinkingTimes()
    {
        return _thinkingTimes.ToDictionary(
            x =>  x.Key,
            x => (IReadOnlyList<TimeSpan>)x.Value.ToList()
        );
    }

    public IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetEatingTimes()
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
}