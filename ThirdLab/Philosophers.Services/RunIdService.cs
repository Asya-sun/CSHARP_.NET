using System.Diagnostics;

namespace Philosophers.Services;

public class RunIdService
{
    private int _currentRunId = 0;
    private readonly object _lock = new object();
    private readonly Stopwatch _simulationTimer = new Stopwatch();
    private DateTime _simulationStartTime;
    private bool _isInitialized = false;

    public int CurrentRunId
    {
        get
        {
            lock (_lock)
            {
                if (_currentRunId == 0)
                {
                    // Автоматически создаем новый RunId при первом обращении
                    _currentRunId = GenerateRunId();
                    _simulationStartTime = DateTime.UtcNow;
                    _simulationTimer.Restart();
                    _isInitialized = true;
                    Console.WriteLine($"Автосоздан RunId: {_currentRunId}");
                }
                return _currentRunId;
            }
        }
    }

    // Метод для явного создания RunId (опционально)
    public int CreateNewRunId()
    {
        lock (_lock)
        {
            _currentRunId = GenerateRunId();
            _simulationStartTime = DateTime.UtcNow;
            _simulationTimer.Restart();
            _isInitialized = true;
            Console.WriteLine($"Создан новый RunId: {_currentRunId}");
            return _currentRunId;
        }
    }

    private int GenerateRunId()
    {
        // Простая генерация на основе времени
        return Math.Abs((int)DateTime.UtcNow.Ticks);
    }

    public TimeSpan GetCurrentSimulationTime()
    {
        return _isInitialized ? _simulationTimer.Elapsed : TimeSpan.Zero;
    }

    public DateTime GetSimulationStartTime()
    {
        return _simulationStartTime;
    }

    public void StopSimulation()
    {
        _simulationTimer.Stop();
    }

    public bool IsSimulationRunning()
    {
        return _simulationTimer.IsRunning;
    }

    public bool IsRunIdSet()
    {
        lock (_lock)
        {
            return _currentRunId != 0;
        }
    }
}