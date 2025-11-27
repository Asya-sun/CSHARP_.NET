using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Interfaces;
using Philosophers.Services;

namespace Philosophers.Services.Philosophers;

public class Socrates : PhilosopherHostedService
{
    public Socrates(
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<Socrates> logger,
        ISimulationRepository repository,
        RunIdService runIdService)
        : base(PhilosopherName.Socrates, tableManager, strategy, metricsCollector, options, logger, repository, runIdService)
    {
    }
}