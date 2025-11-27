using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Interfaces;

namespace Philosophers.Services;

public class DeadlockDetector : BackgroundService
{
    protected readonly ITableManager _tableManager;
    private readonly ILogger<DeadlockDetector> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
    private readonly Random _random = new Random();
    private readonly ISimulationRepository _repository;
    private readonly RunIdService _runIdService;
    private int _deadlockCount = 0;

    public DeadlockDetector(
        ITableManager tableManager,
        ILogger<DeadlockDetector> logger,
        IMetricsCollector metricsCollector,
        ISimulationRepository repository,
        RunIdService runIdService)
    {
        _tableManager = tableManager;
        _logger = logger;
        _metricsCollector = metricsCollector;
        _repository = repository;
        _runIdService = runIdService;

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
                    _deadlockCount++;
                    _logger.LogWarning("ДЕДЛОК! Все философы голодны и все вилки заняты");
                    _metricsCollector.RecordDeadlock();

                    // заставляем философа отпустить вилки
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

    protected async Task ResolveDeadlock()
    {
        var philosophers = _tableManager.GetAllPhilosophers().ToList();

        if (philosophers.Count == 0) return;

        var victim = philosophers[_random.Next(philosophers.Count)];

        _logger.LogWarning("Выбираем философа {Philosopher} для разрешения дедлока", victim.Name);

        var (leftForkId, rightForkId) = _tableManager.GetPhilosopherForks(victim.Name);

        _logger.LogInformation("Философ {Philosopher} принудительно отпускает вилки {LeftFork} и {RightFork}",
            victim.Name, leftForkId, rightForkId);

        await _repository.RecordDeadlockAsync(
                        _runIdService.CurrentRunId,
                        _deadlockCount,
                        _runIdService.GetCurrentSimulationTime(),
                        victim.Name);
        _tableManager.ReleaseFork(leftForkId, victim.Name);
        _tableManager.ReleaseFork(rightForkId, victim.Name);        

        // Даем время другим философам взять вилки
        await Task.Delay(100);

        _logger.LogInformation("Дедлок разрешен! Философ {Philosopher} освободил вилки", victim.Name);
    }
}