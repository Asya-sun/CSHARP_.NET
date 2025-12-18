using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TableService.Interfaces;
using TableService.Models;
using TableService.Models.Enums;
using TableService.Models.TableService.Models;

namespace TableService.Services
{
    public class TableManagerService : ITableManager
    {
        private readonly TableConfig _config;
        private readonly Dictionary<int, SemaphoreSlim> _forks;
        // forkId => philosopherId
        private readonly Dictionary<int, string> _forkOwners;
        // philosopherId => Philosopher
        private readonly ConcurrentDictionary<string, Philosopher> _philosophers = new();
        private int _finishedCount = 0;

        // philosopherId => (left, right)
        private readonly Dictionary<string, (int LeftForkId, int RightForkId)> _philosopherForks; 
        private readonly object _lockObject = new();
        private readonly ILogger<TableManagerService> _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly IHostApplicationLifetime _appLifetime;
        private int _forkNumber = 0;

        public TableManagerService(
            IOptions<TableConfig> config, ILogger<TableManagerService> logger, IMetricsCollector metricsCollector, IHostApplicationLifetime appLifetime)
        {
            _config = config.Value;
            _logger = logger;
            _metricsCollector = metricsCollector;
            _appLifetime = appLifetime;
            if (_config.PhilosophersCount <= 0)
            {
                _logger.LogCritical("PhilosopherCount must be >= 1: got {PhilosophersCount}", _config.PhilosophersCount);
            }

            _forkNumber = (_config.PhilosophersCount == 1) ? 2 : _config.PhilosophersCount;
            _logger.LogDebug("создано вилок: {_forkNumber}", _forkNumber);

            _forks = new Dictionary<int, SemaphoreSlim>();
            for (int i = 1; i <= _forkNumber; i++)
            {
                _forks[i] = new SemaphoreSlim(1, 1);
            }

            _forkOwners = new Dictionary<int, string>();
            _philosophers = new ConcurrentDictionary<string, Philosopher>();
            _philosopherForks = new Dictionary<string, (int, int)>();
        }

        public bool RegisterPhilosopher(string philosopherId, string name, int leftForkId, int rightForkId)
        {
            lock (_lockObject)
            {
                if (_philosophers.ContainsKey(philosopherId))
                {
                    _logger.LogWarning("Философ {PhilosopherId} уже зарегистрирован", philosopherId);
                    return false;
                }

                if (!_forks.ContainsKey(leftForkId) || !_forks.ContainsKey(rightForkId))
                {
                    _logger.LogError("Некорректные ID вилок: {Left}/{Right}", leftForkId, rightForkId);
                    return false;
                }

                _philosophers[philosopherId] = new Philosopher(philosopherId, name)
                {
                    Id = philosopherId,
                    Name = name,
                    State = PhilosopherState.Thinking,
                    Action = "None",
                    EatCount = 0
                };

                _philosopherForks[philosopherId] = (leftForkId, rightForkId);

                _logger.LogInformation("Зарегистрирован философ {Name} (ID: {Id}), вилки: L={Left}, R={Right}",
                    name, philosopherId, leftForkId, rightForkId);
                return true;
            }
        }

        public void UnregisterPhilosopher(string philosopherId)
        {
            if (_philosophers.TryGetValue(philosopherId, out var philosopher))
            {
                philosopher._isFinished = true;
                Interlocked.Increment(ref _finishedCount);

                _logger.LogInformation(
                    "Философ {Name} вышел ({Finished}/{Total})",
                    philosopher.Name,
                    _finishedCount,
                    _config.PhilosophersCount
                );

                if (_finishedCount == _config.PhilosophersCount)
                {
                    _logger.LogInformation("Все философы завершили работу. Печать метрик и остановка сервиса.");

                    _metricsCollector.PrintMetrics();

                    _appLifetime.StopApplication();
                }
            }


        }

        public async Task<bool> WaitForForkAsync(int forkId, string philosopherId, CancellationToken cancellationToken)
        {
            if (!_forks.TryGetValue(forkId, out var semaphore))
            {
                _logger.LogError("Вилка {ForkId} не существует", forkId);
                return false;
            }

            bool acquired = await semaphore.WaitAsync(0, cancellationToken);

            if (acquired)
            {
                lock (_lockObject)
                {
                    _forkOwners[forkId] = philosopherId;
                }
                _metricsCollector.RecordForkAcquired(forkId, philosopherId);
                _logger.LogDebug("Философ {PhilosopherId} взял вилку {ForkId}", philosopherId, forkId);
            }
            else
            {
                _logger.LogDebug("Философ {PhilosopherId} не смог взять вилку {ForkId} (занята)", philosopherId, forkId);
            }

            return acquired;
        }

        public void ReleaseFork(int forkId, string philosopherId)
        {
            if (!_forks.TryGetValue(forkId, out var semaphore))
            {
                _logger.LogError("Вилка {ForkId} не существует", forkId);
                return;
            }

            lock (_lockObject)
            {
                if (_forkOwners.ContainsKey(forkId) && _forkOwners[forkId] == philosopherId)
                {
                    _forkOwners.Remove(forkId);
                    semaphore.Release();
                    _metricsCollector.RecordForkReleased(forkId);
                    _logger.LogDebug("Философ {PhilosopherId} положил вилку {ForkId}", philosopherId, forkId);
                }
                else
                {
                    _logger.LogWarning("Попытка вернуть вилку {ForkId} не её владельцем {PhilosopherId}",
                        forkId, philosopherId);
                }
            }
        }

        public ForkState GetForkState(int forkId)
        {
            lock (_lockObject)
            {
                return _forkOwners.ContainsKey(forkId) ? ForkState.InUse : ForkState.Available;
            }
        }

        public string? GetForkOwner(int forkId)
        {
            lock (_lockObject)
            {
                return _forkOwners.GetValueOrDefault(forkId);
            }
        }

        public (int leftForkId, int rightForkId) GetPhilosopherForks(string philosopherId)
        {
            lock (_lockObject)
            {
                if (!_philosopherForks.TryGetValue(philosopherId, out var forks))
                    throw new ArgumentException($"Философ {philosopherId} не зарегистрирован");

                return forks;
            }
        }

        public IReadOnlyList<Philosopher> GetAllPhilosophers()
        {
            lock (_lockObject)
            {
                return _philosophers.Values.ToList();
            }
        }

        public IReadOnlyList<Fork> GetAllForks()
        {
            var forks = new List<Fork>();

            lock (_lockObject)
            {
                for (int i = 1; i <= _forkNumber; i++)
                {
                    forks.Add(new Fork
                    {
                        _id = i,
                        _state = GetForkState(i),
                        _usedBy = GetForkOwner(i)
                    });
                }
            }

            return forks;
        }

        public (ForkState left, ForkState right) GetAdjacentForksState(string philosopherId)
        {
            var (leftForkId, rightForkId) = GetPhilosopherForks(philosopherId);
            return (GetForkState(leftForkId), GetForkState(rightForkId));
        }

        public void UpdatePhilosopherState(string philosopherId, PhilosopherState state, string action = "None")
        {
            lock (_lockObject)
            {
                if (_philosophers.TryGetValue(philosopherId, out var philosopher))
                {
                    philosopher.State = state;
                    philosopher.Action = action;

                    if (state == PhilosopherState.Eating)
                    {
                        philosopher.EatCount++;
                        _metricsCollector.RecordEating(philosopherId);
                        _logger.LogDebug("Философ {Name} поел. Всего: {Count}",
                            philosopher.Name, philosopher.EatCount);
                    }
                }
                else
                {
                    _logger.LogWarning("Попытка обновить состояние незарегистрированного философа {Id}", philosopherId);
                }
            }
        }

        public Philosopher? GetPhilosopher(string philosopherId)
        {
            lock (_lockObject)
            {
                return _philosophers.GetValueOrDefault(philosopherId);
            }
        }

        public bool IsPhilosopherRegistered(string philosopherId)
        {
            lock (_lockObject)
            {
                return _philosophers.ContainsKey(philosopherId);
            }
        }
    }
}