using Microsoft.Extensions.Logging;
using Philosophers.Core;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Interfaces;
using System.Diagnostics;

namespace Philosophers.Services;

public class TableManager : ITableManager
{
    private readonly Dictionary<int, SemaphoreSlim> _forks;
    private readonly Dictionary<int, PhilosopherName> _forkOwners;
    private readonly Dictionary<PhilosopherName, Philosopher> _philosophers;
    private readonly object _lockObject = new object();
    private readonly ILogger<TableManager> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ISimulationRepository _repository;
    private readonly RunIdService _runIdService;
    protected int _currentRunId;

    public TableManager(ILogger<TableManager> logger, 
        IMetricsCollector metricsCollector,
        ISimulationRepository repository, 
        RunIdService runIdService)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
        _repository = repository;
        _runIdService = runIdService;
        _currentRunId = runIdService.CurrentRunId;

        // Инициализация вилок
        _forks = new Dictionary<int, SemaphoreSlim>
        {
            [1] = new SemaphoreSlim(1, 1),
            [2] = new SemaphoreSlim(1, 1),
            [3] = new SemaphoreSlim(1, 1),
            [4] = new SemaphoreSlim(1, 1),
            [5] = new SemaphoreSlim(1, 1)
        };
        _forkOwners = new Dictionary<int, PhilosopherName>();


        _philosophers = new Dictionary<PhilosopherName, Philosopher>
        {
            [PhilosopherName.Plato] = new Philosopher(PhilosopherName.Plato),
            [PhilosopherName.Kant] = new Philosopher(PhilosopherName.Kant),
            [PhilosopherName.Aristotle] = new Philosopher(PhilosopherName.Aristotle),
            [PhilosopherName.Decartes] = new Philosopher(PhilosopherName.Decartes),
            [PhilosopherName.Socrates] = new Philosopher(PhilosopherName.Socrates)
        };
    }


    public async Task<bool> WaitForForkAsync(int forkId, PhilosopherName philosopherName, CancellationToken cancellationToken, int? timeoutMs = null)
    {
        
        if (! _forks.TryGetValue(forkId, out var semaphore))
        {
            return false;
        }
        
        bool acquired;

        // в случае если поток был отменен до того, как семафор был доступен,
        // WaitAsync выбросит OperationCanceledException (и этот метод тоже выкинет исключение)
        if (timeoutMs == null)
        {
            await semaphore.WaitAsync(cancellationToken);
            // Если дошли сюда - значит взяли семафор
            acquired = true; 
        }
        else if (timeoutMs == 0)
        {
            acquired = await semaphore.WaitAsync(0, cancellationToken);
        }
        else if (timeoutMs > 0)
        {
            acquired = await semaphore.WaitAsync(timeoutMs.Value, cancellationToken);
        }
        else
        {
            throw new ArgumentException("Timeout cannot be negative");
        }

        if (acquired)
        {
            lock (_lockObject)
            {
                _forkOwners[forkId] = philosopherName;
            }
            _metricsCollector.RecordForkAcquired(forkId, philosopherName);
            
            await _repository.RecordForkStateAsync(
            _runIdService.CurrentRunId,
            forkId,
            ForkState.InUse,
            philosopherName,
            _runIdService.GetCurrentSimulationTime());

            _logger.LogDebug("Философ {Philosopher} взял вилку {ForkId}", philosopherName, forkId);
            return true;
        }
        return false;        
    }

    public void ReleaseFork(int forkId, PhilosopherName philosopherName)
    {
        if (_forks.TryGetValue(forkId, out var semaphore))
        {
            lock (_lockObject)
            {
                if (_forkOwners.ContainsKey(forkId) && _forkOwners[forkId] == philosopherName)
                {
                    _forkOwners.Remove(forkId);
                    semaphore.Release();
                    _metricsCollector.RecordForkReleased(forkId);
                    // _ = для fire-and-forget / не ждём завершения
                    _ = _repository.RecordForkStateAsync(
                        _runIdService.CurrentRunId,
                        forkId,
                        ForkState.Available,
                        null, // Вилка свободна
                        _runIdService.GetCurrentSimulationTime());

                    _logger.LogDebug("Философ {Philosopher} положил вилку {ForkId}", philosopherName, forkId);
                }
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

    public PhilosopherName? GetForkOwner(int forkId)
    {
        lock (_lockObject)
        {
            return _forkOwners.GetValueOrDefault(forkId);
        }
    }

    public (int leftForkId, int rightForkId) GetPhilosopherForks(PhilosopherName philosopherName)
    {
        return philosopherName switch
        {


            PhilosopherName.Plato => (1, 5),
            PhilosopherName.Aristotle => (2, 1),
            PhilosopherName.Socrates => (3, 2),
            PhilosopherName.Decartes => (4, 3),
            PhilosopherName.Kant => (5, 4),
            _ => throw new ArgumentException($"Unknown philosopher: {PhilosopherExtensions.ToName(philosopherName)}")
        };
    }

    public IReadOnlyList<Fork> GetAllForks()
    {
        var forks = new List<Fork>();
        for (int i = 1; i <= 5; i++)
        {
            forks.Add(new Fork
            {
                _id = i,
                _state = GetForkState(i),
                _usedBy = GetForkOwner(i)
            });
        }
        return forks;
    }

    public (ForkState left, ForkState right) GetAdjacentForksState(PhilosopherName philosopherName)
    {
        var (leftForkId, rightForkId) = GetPhilosopherForks(philosopherName);
        return (GetForkState(leftForkId), GetForkState(rightForkId));
    }


    public void UpdatePhilosopherState(PhilosopherName name, PhilosopherState state, string action = "None")
    {
        lock (_lockObject)
        {
            if (_philosophers.ContainsKey(name))
            {
                _philosophers[name].State = state;
                _philosophers[name].Action = action;
            }
        }
    }

    public IReadOnlyList<Philosopher> GetAllPhilosophers()
    {
        lock (_lockObject)
        {
            return _philosophers.Values.ToList();
        }
    }


}