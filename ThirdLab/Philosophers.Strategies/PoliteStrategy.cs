using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models;
using Philosophers.Core;
using Microsoft.Extensions.Options;
using System.Text;

namespace Philosophers.Strategies;

public class PoliteStrategy : IPhilosopherStrategy
{
    private readonly ILogger<PoliteStrategy> _logger;
    private readonly SimulationOptions _options;

    public PoliteStrategy(ILogger<PoliteStrategy> logger, IOptions<SimulationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> TryAcquireForksAsync(PhilosopherName philosopherName, ITableManager tableManager, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        _logger.LogDebug("Философ {Philosopher} пытается взять левую вилку {LeftFork}", philosopherName, leftForkId);

        bool leftAcquired = await tableManager.WaitForForkAsync(leftForkId, philosopherName, cancellationToken, 0);
        if (!leftAcquired)
        {
            _logger.LogDebug("Философ {Philosopher} не смог взять левую вилку {LeftFork}", philosopherName, leftForkId);
            return false;
        }

        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogDebug("Философ {Philosopher} пытается взять правую вилку {RightFork}", PhilosopherExtensions.ToName(philosopherName), rightForkId);

        bool rightAcquired = await tableManager.WaitForForkAsync(rightForkId, philosopherName, cancellationToken, 0);
        if (!rightAcquired)
        {
            _logger.LogDebug("Философ {Philosopher} не смог взять правую вилку {RightFork}, отпускает левую", PhilosopherExtensions.ToName(philosopherName), rightForkId);
            tableManager.ReleaseFork(leftForkId, philosopherName);
            return false;
        }

        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogInformation("Философ {Philosopher} успешно взял обе вилки", PhilosopherExtensions.ToName(philosopherName));
        return true;
    }

    public void ReleaseForks(PhilosopherName philosopherName, ITableManager tableManager)
    {
        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        tableManager.ReleaseFork(leftForkId, philosopherName);
        tableManager.ReleaseFork(rightForkId, philosopherName);

        _logger.LogDebug("Философ {Philosopher} положил обе вилки", PhilosopherExtensions.ToName(philosopherName));
    }
}