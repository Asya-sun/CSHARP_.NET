using TableService.Interfaces;
using TableService.Models.Enums;

namespace TableService.Services
{
    public class DeadlockDetector : BackgroundService
    {
        protected readonly ITableManager _tableManager;
        private readonly ILogger<DeadlockDetector> _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
        private int _deadlockCount = 0;

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
                        _deadlockCount++;
                        _logger.LogWarning("ДЕДЛОК! Все философы голодны и все вилки заняты");
                        _metricsCollector.RecordDeadlock();

                        // заставляем философа отпустить вилки
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
    }
}
