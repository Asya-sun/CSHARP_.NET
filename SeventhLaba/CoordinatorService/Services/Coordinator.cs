using CoordinatorService.Interfaces;
using CoordinatorService.Models;
using MassTransit;
using Philosophers.Shared;
using Philosophers.Shared.Events;

namespace CoordinatorService.Services;

public class Coordinator : ICoordinator
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Coordinator> _logger;
    private readonly CoordinatorState _state;

    public Coordinator(
        IPublishEndpoint publishEndpoint,
        CoordinatorState state,
        ILogger<Coordinator> logger)
    {
        _publishEndpoint = publishEndpoint;
        _state = state;
        _logger = logger;
    }

    public async Task RequestToEatAsync(string philosopherId)
    {
        bool canEatImmediately;

        lock (_state.Lock)
        {
            canEatImmediately = !_state.SomeoneEating;

            if (canEatImmediately)
            {
                _state.SomeoneEating = true;
                _logger.LogInformation(
                    "CoordinatorService: разрешаю есть философу {Id}",
                    philosopherId);
            }
            else
            {
                _state.Queue.Enqueue(philosopherId);
                _logger.LogInformation(
                    "CoordinatorService: философ {Id} добавлен в очередь",
                    philosopherId);
            }
        }

        if (canEatImmediately)
        {
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = philosopherId
            });
        }
    }

    public async Task FinishedEatingAsync(string philosopherId)
    {
        string? next = null;

        lock (_state.Lock)
        {
            if (_state.Queue.Any())
            {
                next = _state.Queue.Dequeue();
                _logger.LogInformation(
                    "CoordinatorService: следующий философ {Id}",
                    next);
            }
            else
            {
                _state.SomeoneEating = false;
                _logger.LogInformation(
                    "CoordinatorService: стол снова свободен");
            }
        }

        if (next != null)
        {
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = next
            });
        }
    }

}
