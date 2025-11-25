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
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
    private readonly Random _random = new Random();

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
        _logger.LogInformation("Детектор дедлоков запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                if (CheckForDeadlock())
                {
                    _logger.LogWarning("ДЕДЛОК! Все философы голодны и все вилки заняты");
                    _metricsCollector.RecordDeadlock();

                    // СПАСАЕМ СИТУАЦИЮ - заставляем философа отпустить вилки
                    await ResolveDeadlock();
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

        _logger.LogInformation("Детектор дедлоков остановлен");
    }

    internal bool CheckForDeadlock()
    {
        var philosophers = _tableManager.GetAllPhilosophers();
        var forks = _tableManager.GetAllForks();

        bool allPhilosophersHungry = philosophers.All(p => p.State == PhilosopherState.Hungry);
        bool allForksInUse = forks.All(f => f._state == ForkState.InUse);

        return allPhilosophersHungry && allForksInUse;
    }

    private async Task ResolveDeadlock()
    {
        var philosophers = _tableManager.GetAllPhilosophers().ToList();

        if (philosophers.Count == 0) return;

        // Выбираем случайного философа для "жертвоприношения"
        var victim = philosophers[_random.Next(philosophers.Count)];

        _logger.LogWarning("Выбираем философа {Philosopher} для разрешения дедлока", victim.Name);

        // Получаем вилки этого философа
        var (leftForkId, rightForkId) = _tableManager.GetPhilosopherForks(victim.Name);

        // Заставляем отпустить левую вилку (или обе)
        _logger.LogInformation("Философ {Philosopher} принудительно отпускает вилки {LeftFork} и {RightFork}",
            victim.Name, leftForkId, rightForkId);

        // Отпускаем вилки через TableManager
        _tableManager.ReleaseFork(leftForkId, victim.Name);
        _tableManager.ReleaseFork(rightForkId, victim.Name);

        // Даем время другим философам взять вилки
        await Task.Delay(100);

        _logger.LogInformation("Дедлок разрешен! Философ {Philosopher} освободил вилки", victim.Name);
    }
}