using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Services;

public class DeadlockDetector : BackgroundService
{
    private readonly ITableManager _tableManager;
    private readonly ILogger<DeadlockDetector> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5); // Проверяем каждые 5 секунд

    public DeadlockDetector(
        ITableManager tableManager,
        ILogger<DeadlockDetector> logger,
        IMetricsCollector metricsCollector)
    {
        _tableManager = tableManager;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔍 Детектор дедлоков запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ждем перед следующей проверкой
                await Task.Delay(_checkInterval, stoppingToken);

                // Проверяем на дедлоки
                if (CheckForDeadlock())
                {
                    _logger.LogWarning("🚨 ОБНАРУЖЕН ДЕДЛОК! Все философы голодны и все вилки заняты");
                    _metricsCollector.RecordDeadlock();

                    // Здесь можно добавить логику "спасения" от дедлока
                    // Например: заставить одного философа отпустить вилки
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в детекторе дедлоков");
            }
        }

        _logger.LogInformation("🔍 Детектор дедлоков остановлен");
    }

    private bool CheckForDeadlock()
    {
        // Получаем текущее состояние стола
        var philosophers = _tableManager.GetAllPhilosophers();
        var forks = _tableManager.GetAllForks();

        // Условия дедлока:
        // 1. ВСЕ философы в состоянии "Голоден" (Hungry)
        bool allPhilosophersHungry = philosophers.All(p => p.State == PhilosopherState.Hungry);

        // 2. ВСЕ вилки заняты (InUse)
        bool allForksInUse = forks.All(f => f._state == ForkState.InUse);

        // Если оба условия true - у нас дедлок!
        return allPhilosophersHungry && allForksInUse;
    }
}