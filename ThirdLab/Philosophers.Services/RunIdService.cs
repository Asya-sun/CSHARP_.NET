using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Services;

public class RunIdService
{
    public Guid _currentRunId { get; private set; }

    private readonly object _lock = new object();
    private readonly Stopwatch _simulationTimer = new Stopwatch();
    private DateTime _simulationStartTime;
    public Guid CurrentRunId
    {
        get
        {
            lock (_lock)
            {
                if (_currentRunId == Guid.Empty)
                {
                    // Автоматически создаем RunId если его нет
                    _currentRunId = Guid.NewGuid();
                    _simulationStartTime = DateTime.UtcNow;
                    _simulationTimer.Restart();
                    Console.WriteLine($"Auto-created RunId: {_currentRunId}");
                }
                return _currentRunId;
            }
        }
    }

    public void StartSimulation(Guid runId)
    {
        lock (_lock)
        {
            _currentRunId = runId;
            _simulationStartTime = DateTime.UtcNow;
            _simulationTimer.Restart();
        }
    }

    //public void StartSimulation(Guid runId)
    //{
    //    CurrentRunId = runId;
    //    _simulationStartTime = DateTime.UtcNow;
    //    _simulationTimer.Restart();
    //}

    public TimeSpan GetCurrentSimulationTime()
    {
        return _simulationTimer.Elapsed;
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
}