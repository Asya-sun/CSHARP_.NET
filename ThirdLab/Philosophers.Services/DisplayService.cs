using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using Philosophers.Core.Models;
using System.Text;

namespace Philosophers.Services;

public class DisplayService : BackgroundService
{
    private readonly ITableManager _tableManager;
    private readonly SimulationOptions _options;
    private readonly ILogger<DisplayService> _logger;
    private int _step = 0;

    public DisplayService(ITableManager tableManager, IOptions<SimulationOptions> options, ILogger<DisplayService> logger)
    {
        _tableManager = tableManager;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DisplayService запущен");

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
    }

    private void DisplayCurrentState()
    {
        var philosophers = _tableManager.GetAllPhilosophers();
        var forks = _tableManager.GetAllForks();

        _logger.LogInformation("===== ШАГ {Step} =====", _step);

        _logger.LogInformation("Философы:");
        foreach (var philosopher in philosophers)
        {
            var stateText = philosopher.State switch
            {
                PhilosopherState.Eating => $"Eating ({philosopher.StepsLeft} steps left)",
                PhilosopherState.Thinking => $"Thinking ({philosopher.StepsLeft} steps left)",
                PhilosopherState.Hungry => $"Hungry (Action = {philosopher.Action})",
                _ => philosopher.State.ToString()
            };

            _logger.LogInformation("  {Name}: {State}, съедено: {EatCount}",
                philosopher.Name, stateText, philosopher.EatCount);
        }

        _logger.LogInformation("Вилки:");
        foreach (var fork in forks)
        {
            var usedBy = fork._state == ForkState.InUse ? $"(используется {fork._usedBy})" : "";
            _logger.LogInformation("  Fork-{ForkId}: {State} {UsedBy}",
                fork._id, fork._state, usedBy);
        }

        _logger.LogInformation("");
    }
}