using Microsoft.Extensions.Logging;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System.Diagnostics;

namespace Philosophers.Services;

public class TableManager : ITableManager
{
    private readonly Dictionary<int, SemaphoreSlim> _forks;
    private readonly Dictionary<int, string> _forkOwners;
    private readonly Dictionary<string, Philosopher> _philosophers;
    private readonly object _lockObject = new object();
    private readonly ILogger<TableManager> _logger;
    private readonly IMetricsCollector _metricsCollector; // ← ДОБАВЛЯЕМ

    public TableManager(ILogger<TableManager> logger, IMetricsCollector metricsCollector) // ← ДОБАВЛЯЕМ
    {
        _logger = logger;
        _metricsCollector = metricsCollector;

        // Инициализация вилок
        _forks = new Dictionary<int, SemaphoreSlim>
        {
            [1] = new SemaphoreSlim(1, 1),
            [2] = new SemaphoreSlim(1, 1),
            [3] = new SemaphoreSlim(1, 1),
            [4] = new SemaphoreSlim(1, 1),
            [5] = new SemaphoreSlim(1, 1)
        };
        _forkOwners = new Dictionary<int, string>();


        _philosophers = new Dictionary<string, Philosopher>
        {
            ["Платон"] = new Philosopher("Платон"),
            ["Аристотель"] = new Philosopher("Аристотель"),
            ["Сократ"] = new Philosopher("Сократ"),
            ["Декарт"] = new Philosopher("Декарт"),
            ["Кант"] = new Philosopher("Кант")
        };
    }

    //public async Task<bool> TryAcquireForkAsync(int forkId, string philosopherName, CancellationToken cancellationToken)
    //{
    //    if (_forks.TryGetValue(forkId, out var semaphore))
    //    {
    //        var acquired = await semaphore.WaitAsync(0, cancellationToken);
    //        if (acquired)
    //        {
    //            lock (_lockObject)
    //            {
    //                _forkOwners[forkId] = philosopherName;
    //            }
    //            _metricsCollector.RecordForkAcquired(forkId, philosopherName); // ← ЗАПИСЫВАЕМ МЕТРИКУ
    //            _logger.LogDebug("Философ {Philosopher} взял вилку {ForkId}", philosopherName, forkId);
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    public async Task<bool> WaitForForkAsync(int forkId, string philosopherName, CancellationToken cancellationToken, int? timeoutMs = null)
    {
        // инвертировать if
        if (_forks.TryGetValue(forkId, out var semaphore))
        {
            bool acquired;

            if (timeoutMs == null)
            {
                // Бесконечное ожидание - используем WaitAsync() без возвращаемого значения
                await semaphore.WaitAsync(cancellationToken);
                acquired = true; // Если дошли сюда - значит взяли семафор
            }
            else if (timeoutMs == 0)
            {
                // Мгновенная проверка
                acquired = await semaphore.WaitAsync(0, cancellationToken);
            }
            else if (timeoutMs > 0)
            {
                // Ожидание с таймаутом
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
                _logger.LogDebug("Философ {Philosopher} взял вилку {ForkId}", philosopherName, forkId);
                return true;
            }
        }
        return false;
    }

    public void ReleaseFork(int forkId, string philosopherName)
    {
        if (_forks.TryGetValue(forkId, out var semaphore))
        {
            lock (_lockObject)
            {
                if (_forkOwners.ContainsKey(forkId) && _forkOwners[forkId] == philosopherName)
                {
                    _forkOwners.Remove(forkId);
                    semaphore.Release();
                    _metricsCollector.RecordForkReleased(forkId); // ← ЗАПИСЫВАЕМ МЕТРИКУ
                    _logger.LogDebug("Философ {Philosopher} положил вилку {ForkId}", philosopherName, forkId);
                }
            }
        }
    }

    // Остальные методы остаются без изменений
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

    public (int leftForkId, int rightForkId) GetPhilosopherForks(string philosopherName)
    {
        return philosopherName switch
        {
            "Платон" => (1, 5),
            "Аристотель" => (2, 1),
            "Сократ" => (3, 2),
            "Декарт" => (4, 3),
            "Кант" => (5, 4),
            _ => throw new ArgumentException($"Unknown philosopher: {philosopherName}")
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

    public (ForkState left, ForkState right) GetAdjacentForksState(string philosopherName)
    {
        var (leftForkId, rightForkId) = GetPhilosopherForks(philosopherName);
        return (GetForkState(leftForkId), GetForkState(rightForkId));
    }


    public void UpdatePhilosopherState(string name, PhilosopherState state, string action = "None")
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