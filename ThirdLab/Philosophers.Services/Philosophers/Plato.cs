using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Services;

namespace Philosophers.Services.Philosophers;

public class Plato : PhilosopherHostedService
{
    public Plato(
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<Plato> logger)
        : base(PhilosopherName.Plato, tableManager, strategy, metricsCollector, options, logger)
    {
    }
}