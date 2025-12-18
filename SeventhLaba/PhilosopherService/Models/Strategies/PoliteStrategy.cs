using Microsoft.Extensions.Options;
using Philosophers.Shared.DTO;
using PhilosopherService.Http;
using PhilosopherService.Interfaces;
using System.Text;

namespace PhilosopherService.Models.Strategies;

public class PoliteStrategy : IPhilosopherStrategy
{
    private readonly ILogger<PoliteStrategy> _logger;

    public PoliteStrategy(ILogger<PoliteStrategy> logger)
    {
        _logger = logger;
    }

    public async Task AcquireForksAsync(
    PhilosopherConfig config,
    TableClient tableClient,
    CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // левая
            if (!await tableClient.TryAcquireForkAsync(
                    new AcquireForkRequest(config.PhilosopherId, config.LeftForkId),
                    cancellationToken))
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            await Task.Delay(100, cancellationToken);

            // правая
            if (!await tableClient.TryAcquireForkAsync(
                    new AcquireForkRequest(config.PhilosopherId, config.RightForkId),
                    cancellationToken))
            {
                await tableClient.ReleaseForkAsync(
                    new ReleaseForkRequest(config.PhilosopherId, config.LeftForkId));

                await Task.Delay(100, cancellationToken);
                continue;
            }

            // обе взяты
            _logger.LogDebug("Философ {Name} взял обе вилки", config.Name);
            return;
        }
    }


    public async Task ReleaseForksAsync(
        PhilosopherConfig config,
        TableClient tableClient)
    {
        await Task.WhenAll(
            tableClient.ReleaseForkAsync(
                new ReleaseForkRequest(config.PhilosopherId, config.LeftForkId)),
            tableClient.ReleaseForkAsync(
                new ReleaseForkRequest(config.PhilosopherId, config.RightForkId))
        );
    }
}
