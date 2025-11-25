using Philosophers.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models;
using Microsoft.Extensions.Options;

namespace Philosophers.Strategies;

public class StupidStrategy : IPhilosopherStrategy
{
    private readonly ILogger<StupidStrategy> _logger;
    private readonly SimulationOptions _options;

    public StupidStrategy(ILogger<StupidStrategy> logger, IOptions<SimulationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> TryAcquireForksAsync(PhilosopherName philosopherName, ITableManager tableManager, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        _logger.LogDebug("Философ {Philosopher} ТУПО берет левую вилку {LeftFork} (и не отпустит!)",
            PhilosopherExtensions.ToName(philosopherName), leftForkId);

        // бесконечное ожидание на левой вилке
        bool leftAcquired = await tableManager.WaitForForkAsync(leftForkId, philosopherName, cancellationToken);
        if (!leftAcquired)
        {
            _logger.LogDebug("Философ {Philosopher} не смог взять левую вилку (странно...)", PhilosopherExtensions.ToName(philosopherName));
            return false;
        }

        // Имитируем время взятия вилки
        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogDebug("Философ {Philosopher} взял левую, ждет правую {RightFork}",
            PhilosopherExtensions.ToName(philosopherName), rightForkId);

        // бесконечное ожидание на правой вилке
        bool rightAcquired = await tableManager.WaitForForkAsync(rightForkId, philosopherName, cancellationToken);

        if (!rightAcquired)
        {
            // Этого никогда не должно случиться с бесконечным ожиданием,
            // но на случай отмены операции
            _logger.LogDebug("Философ {Philosopher} не смог взять правую вилку (отмена?)", PhilosopherExtensions.ToName(philosopherName));
            tableManager.ReleaseFork(leftForkId, philosopherName);
            return false;
        }

        // Имитируем время взятия второй вилки
        await Task.Delay(_options.ForkAcquisitionTime, cancellationToken);

        _logger.LogInformation("Философ {Philosopher} ЧУДОМ взял обе вилки (дедлок избегнут!)", PhilosopherExtensions.ToName(philosopherName));
        return true;
    }

    public void ReleaseForks(PhilosopherName philosopherName, ITableManager tableManager)
    {
        var (leftForkId, rightForkId) = tableManager.GetPhilosopherForks(philosopherName);

        tableManager.ReleaseFork(leftForkId, philosopherName);
        tableManager.ReleaseFork(rightForkId, philosopherName);

        _logger.LogDebug("Философ {Philosopher} положил вилки", PhilosopherExtensions.ToName(philosopherName));
    }
}