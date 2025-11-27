using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Interfaces;
using Philosophers.Services;

namespace Philosophers.Services.Philosophers;

public class Kant : PhilosopherHostedService
{
    public Kant(
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<Kant> logger,
        ISimulationRepository repository,
        RunIdService runIdService)
        : base(PhilosopherName.Kant, tableManager, strategy, metricsCollector, options, logger, repository, runIdService)
    {
    }
}
