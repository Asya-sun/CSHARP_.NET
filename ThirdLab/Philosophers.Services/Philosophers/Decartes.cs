using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
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
        ILogger<Decartes> logger)
        : base("Декарт", tableManager, strategy, metricsCollector, options, logger)
    {
    }
}