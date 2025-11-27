using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Services;

public class RunIdService
{
    public Guid CurrentRunId { get; private set; }
    private readonly Stopwatch _simulationTimer = new Stopwatch();
    private DateTime _simulationStartTime;


    public void StartSimulation(Guid runId)
    {
        CurrentRunId = runId;
        _simulationStartTime = DateTime.UtcNow;
        _simulationTimer.Restart();
    }

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