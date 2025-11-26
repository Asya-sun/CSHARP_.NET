using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;

namespace Philosophers.Services;

public class SimulationHostedService : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly SimulationOptions _options;
    private readonly ILogger<SimulationHostedService> _logger;
    private readonly IMetricsCollector _metricsCollector;

    public SimulationHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<SimulationOptions> options,
        ILogger<SimulationHostedService> logger,
        IMetricsCollector metricsCollector)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _options = options.Value;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Симуляция запущена. Длительность: {DurationSeconds} секунд", _options.DurationSeconds);

        await Task.Delay(TimeSpan.FromSeconds(_options.DurationSeconds), stoppingToken);

        _logger.LogInformation("Время симуляции истекло. Завершаем...");

        _metricsCollector.PrintMetrics();

        _hostApplicationLifetime.StopApplication();
    }
}