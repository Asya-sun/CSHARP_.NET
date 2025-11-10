using Philosophers.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

using System.Collections.Concurrent;
using Philosophers.Core.Models;
using Microsoft.Extensions.Options;

namespace Philosophers.Services;

public class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, int> _eatCount = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<TimeSpan>> _waitingTimes = new();
    private readonly ConcurrentDictionary<int, TimeSpan> _forkUsage = new();
    private readonly SimulationOptions _options;

    public MetricsCollector(ILogger<MetricsCollector> logger, IOptions<SimulationOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Инициализация для вилок
        for (int i = 1; i <= 5; i++)
        {
            _forkUsage[i] = TimeSpan.Zero;
        }
    }

    public void RecordEating(string philosopherName)
    {
        // Атомарно увеличиваем счетчик
        _eatCount.AddOrUpdate(philosopherName, 1, (key, oldValue) => oldValue + 1);

        _logger.LogDebug("Записан прием пищи для {Philosopher}", philosopherName);
    }

    public void RecordWaitingTime(string philosopherName, TimeSpan waitingTime)
    {
        // Получаем или создаем коллекцию для философа
        var bag = _waitingTimes.GetOrAdd(philosopherName,
            new ConcurrentBag<TimeSpan>());

        // Добавляем время ожидания (ConcurrentBag потокобезопасен)
        bag.Add(waitingTime);

        _logger.LogDebug("Записано время ожидания для {Philosopher}: {Time} мс",
            philosopherName, waitingTime.TotalMilliseconds);
    }

    public void RecordForkUsage(int forkId, TimeSpan usageTime)
    {
        // Атомарно обновляем время использования вилки
        _forkUsage.AddOrUpdate(forkId, usageTime, (key, oldValue) => oldValue + usageTime);
    }

    public void PrintMetrics()
    {
        _logger.LogInformation("=== МЕТРИКИ СИМУЛЯЦИИ ===");

        // Пропускная способность
        PrintThroughputMetrics();

        // Время ожидания
        PrintWaitingTimeMetrics();

        // Коэффициент утилизации вилок
        PrintForkUtilizationMetrics();
    }

    private void PrintThroughputMetrics()
    {
        _logger.LogInformation("ПРОПУСКНАЯ СПОСОБНОСТЬ:");
        int totalEatCount = 0;
        int philosopherCount = 0;

        foreach (var (philosopher, count) in _eatCount)
        {
            _logger.LogInformation("  {Philosopher}: {Count} раз", philosopher, count);
            totalEatCount += count;
            philosopherCount++;
        }

        if (philosopherCount > 0)
        {
            _logger.LogInformation("  СРЕДНЕЕ: {Average} раз", totalEatCount / philosopherCount);
        }
    }

    private void PrintWaitingTimeMetrics()
    {
        _logger.LogInformation("ВРЕМЯ ОЖИДАНИЯ:");
        TimeSpan maxWaitingTime = TimeSpan.Zero;
        string maxWaitingPhilosopher = "";

        foreach (var (philosopher, times) in _waitingTimes)
        {
            if (times.Any())
            {
                var average = TimeSpan.FromMilliseconds(times.Average(t => t.TotalMilliseconds));
                var max = times.Max();

                _logger.LogInformation("  {Philosopher}: среднее {Average:F0} мс, макс {Max:F0} мс",
                    philosopher, average.TotalMilliseconds, max.TotalMilliseconds);

                if (max > maxWaitingTime)
                {
                    maxWaitingTime = max;
                    maxWaitingPhilosopher = philosopher;
                }
            }
            else
            {
                _logger.LogInformation("  {Philosopher}: не было периодов ожидания", philosopher);
            }
        }

        if (maxWaitingTime > TimeSpan.Zero)
        {
            _logger.LogInformation("  МАКСИМАЛЬНОЕ: {Philosopher} - {Time:F0} мс",
                maxWaitingPhilosopher, maxWaitingTime.TotalMilliseconds);
        }
    }

    private void PrintForkUtilizationMetrics()
    {
        _logger.LogInformation("КОЭФФИЦИЕНТ УТИЛИЗАЦИИ ВИЛОК:");

        // Используем значение из конфига вместо 120
        var totalSimulationTime = TimeSpan.FromSeconds(_options.DurationSeconds);

        foreach (var (forkId, usageTime) in _forkUsage)
        {
            var utilization = (usageTime.TotalMilliseconds / totalSimulationTime.TotalMilliseconds) * 100;
            _logger.LogInformation("  Вилка-{ForkId}: {Utilization:F2}%", forkId, utilization);
        }
    }

}