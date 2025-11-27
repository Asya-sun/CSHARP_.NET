using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Interfaces;
using Philosophers.Services;

namespace Philosophers.Services.Philosophers;

public class Aristotle : PhilosopherHostedService
{
    public Aristotle(
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<Aristotle> logger,
        ISimulationRepository repository,
        RunIdService runIdService
        )
        : base(PhilosopherName.Aristotle, tableManager, strategy, metricsCollector, options, logger, repository, runIdService)
    {
    }
}