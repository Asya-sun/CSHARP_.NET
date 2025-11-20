using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace Philosophers.Strategies;

public class LeftRightStrategy : IPhilosopherStrategy
{
    private readonly ILogger<LeftRightStrategy> _logger;
    private readonly SimulationOptions _options;

    public LeftRightStrategy(ILogger<LeftRightStrategy> logger, IOptions<SimulationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> TryAcquireForksAsync(string philosopherName, ITableManager tableManager, CancellationToken cancellationToken)
    {
        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        _logger.LogDebug("Философ {Philosopher} пытается взять левую вилку {LeftFork}", philosopherName, leftForkId);

        // Пытаемся взять левую вилку
        bool leftAcquired = await tableManager.TryAcquireForkAsync(leftForkId, philosopherName, cancellationToken);
        if (!leftAcquired)
        {
            _logger.LogDebug("Философ {Philosopher} не смог взять левую вилку {LeftFork}", philosopherName, leftForkId);
            return false;
        }

        // Имитируем время взятия вилки
        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogDebug("Философ {Philosopher} пытается взять правую вилку {RightFork}", philosopherName, rightForkId);

        // Пытаемся взять правую вилку
        bool rightAcquired = await tableManager.TryAcquireForkAsync(rightForkId, philosopherName, cancellationToken);
        if (!rightAcquired)
        {
            _logger.LogDebug("Философ {Philosopher} не смог взять правую вилку {RightFork}, отпускает левую", philosopherName, rightForkId);
            tableManager.ReleaseFork(leftForkId, philosopherName);
            return false;
        }

        // Имитируем время взятия второй вилки
        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogInformation("Философ {Philosopher} успешно взял обе вилки", philosopherName);
        return true;
    }

    public void ReleaseForks(string philosopherName, ITableManager tableManager)
    {
        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        tableManager.ReleaseFork(leftForkId, philosopherName);
        tableManager.ReleaseFork(rightForkId, philosopherName);

        _logger.LogDebug("Философ {Philosopher} положил обе вилки", philosopherName);
    }
}