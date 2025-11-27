using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Interfaces;
using Philosophers.Services;
using System.Text;

namespace Philosophers.Services.Philosophers;

public class Decartes : PhilosopherHostedService
{
    public Decartes(
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<Decartes> logger,
        ISimulationRepository repository,
        RunIdService runIdService)
        : base(PhilosopherName.Decartes, tableManager, strategy, metricsCollector, options, logger, repository, runIdService)
    {
    }
}