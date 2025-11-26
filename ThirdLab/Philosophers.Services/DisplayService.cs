using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System.Diagnostics;
using System.Text;

namespace Philosophers.Services;

public class DisplayService : BackgroundService
{
    private readonly ITableManager _tableManager;
    private readonly SimulationOptions _options;
    private readonly ILogger<DisplayService> _logger;
    private int _step = 0;
    private readonly IMetricsCollector _metricsCollector;
    private readonly Stopwatch _simulationTimer;

    public DisplayService(ITableManager tableManager, IMetricsCollector metricsCollector, IOptions<SimulationOptions> options, ILogger<DisplayService> logger)
    {
        _tableManager = tableManager;
        _options = options.Value;
        _logger = logger;
        _metricsCollector = metricsCollector;
        _simulationTimer = new Stopwatch();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DisplayService запущен");
        _simulationTimer.Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.DisplayUpdateInterval, stoppingToken);
                DisplayCurrentState();
                _step++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _simulationTimer.Stop();
    }

    private void DisplayCurrentState()
    {
        var philosophers = _tableManager.GetAllPhilosophers();
        var forks = _tableManager.GetAllForks();

        var sb = new StringBuilder();
        sb.AppendLine($"=== Время симуляции: {_simulationTimer.Elapsed:mm\\:ss} ===");

        sb.AppendLine("Философы:");
        foreach (var philosopher in philosophers)
        {
            var stateText = philosopher.State switch
            {
                PhilosopherState.Eating => "Eating",
                PhilosopherState.Thinking => "Thinking",
                PhilosopherState.Hungry => $"Hungry (Action = {philosopher.Action})",
                _ => philosopher.State.ToString()
            };

            var eatCount = _metricsCollector.GetEatCount(philosopher.Name);
            sb.AppendLine($"  {philosopher.Name}: {stateText}, съедено: {eatCount}");
        }

        sb.AppendLine("Вилки:");
        foreach (var fork in forks)
        {
            var usedBy = fork._state == ForkState.InUse ? $"(используется {fork._usedBy})" : "";
            sb.AppendLine($"  Fork-{fork._id}: {fork._state} {usedBy}");
        }

        _logger.LogInformation("{DisplayState}", sb.ToString());
    }

}