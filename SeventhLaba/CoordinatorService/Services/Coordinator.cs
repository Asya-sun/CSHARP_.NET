using CoordinatorService.Interfaces;
using MassTransit;
using Philosophers.Shared;
using Philosophers.Shared.Events;

namespace CoordinatorService.Services;

public class Coordinator : ICoordinator
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Coordinator> _logger;

    private bool _someoneEating = false;
    private readonly Queue<string> _queue = new();

    private readonly object _lock = new();

    public Coordinator(
        IPublishEndpoint publishEndpoint,
        ILogger<Coordinator> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task RequestToEatAsync(string philosopherId)
    {
        bool canEatImmediately;

        lock (_lock)
        {
            canEatImmediately = !_someoneEating;

            if (canEatImmediately)
            {
                _someoneEating = true;
                _logger.LogInformation(
                    "CoordinatorService: разрешаю есть философу {Id}",
                    philosopherId);
            }
            else
            {
                _queue.Enqueue(philosopherId);
                _logger.LogInformation(
                    "CoordinatorService: философ {Id} добавлен в очередь",
                    philosopherId);
            }
        }

        if (canEatImmediately)
        {
            // Publish выполняется вне lock
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = philosopherId
            });
        }
    }

    public async Task FinishedEatingAsync(string philosopherId)
    {
        string? next = null;

        lock (_lock)
        {
            if (_queue.Any())
            {
                next = _queue.Dequeue();
                _logger.LogInformation(
                    "CoordinatorService: следующий философ {Id}",
                    next);
            }
            else
            {
                _someoneEating = false;
                _logger.LogInformation(
                    "CoordinatorService: стол снова свободен");
            }
        }

        if (next != null)
        {
            // Publish выполняется вне lock
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = next
            });
        }
    }

}
