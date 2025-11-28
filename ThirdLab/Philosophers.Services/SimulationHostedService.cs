using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Interfaces;

namespace Philosophers.Services;

public class SimulationHostedService : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly SimulationOptions _options;
    private readonly ILogger<SimulationHostedService> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ISimulationRepository _repository;
    private readonly RunIdService _runIdService;
    protected Guid _currentRunId;

    public SimulationHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<SimulationOptions> options,
        ILogger<SimulationHostedService> logger,
        IMetricsCollector metricsCollector,
        ISimulationRepository repository,
        RunIdService runIdService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _options = options.Value;
        _logger = logger;
        _metricsCollector = metricsCollector;
        _repository = repository;
        _runIdService = runIdService;
        //_currentRunId = runIdService.CurrentRunId;
        
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runId = await _repository.StartNewRunAsync(_options);
        _currentRunId = runId;
        _runIdService.StartSimulation(runId);
        _logger.LogInformation("Симуляция запущена. Длительность: {DurationSeconds} секунд, RunId: {}RunId", _options.DurationSeconds, _currentRunId);
        _currentRunId = runId;

        await Task.Delay(TimeSpan.FromSeconds(_options.DurationSeconds), stoppingToken);

        _logger.LogInformation("Время симуляции истекло. Завершаем...");

        _runIdService.StopSimulation();
        await _repository.CompleteRunAsync(runId);
        _logger.LogInformation("Симуляция {RunId} завершена", runId);

        _metricsCollector.PrintMetrics();

        _hostApplicationLifetime.StopApplication();
    }
}