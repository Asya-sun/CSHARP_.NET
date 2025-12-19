using CoordinatorService.Interfaces;
using CoordinatorService.Models;
using MassTransit;
using Microsoft.Extensions.Options;
using Philosophers.Shared;
using Philosophers.Shared.Events;
using System.Threading;


namespace CoordinatorService.Services;

public class Coordinator : ICoordinator
{
    private readonly CoordinatorConfig _config;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Coordinator> _logger;
    private readonly CoordinatorState _state;
    private int _finishedPhilosophers = 0;
    private readonly IHostApplicationLifetime _appLifetime;

    public Coordinator(IOptions<CoordinatorConfig> config,
        IPublishEndpoint publishEndpoint,
        CoordinatorState state,
        ILogger<Coordinator> logger,
        IHostApplicationLifetime appLifetime)
    {
        _config = config.Value;
        _publishEndpoint = publishEndpoint;
        _state = state;
        _logger = logger;
        _appLifetime = appLifetime;
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


    public async Task PhilosopherExitingAsync(string philosopherId)
    {
        string? next = null;

        var finished = Interlocked.Increment(ref _finishedPhilosophers);


        lock (_state.Lock)
        {
            // удаляем философа из очереди, если он там есть
            var queue = new Queue<string>(_state.Queue.Where(id => id != philosopherId));
            _state.Queue.Clear();
            foreach (var id in queue)
                _state.Queue.Enqueue(id);

            // если философ был текущим едящим, освобождаем стол
            if (_state.SomeoneEating && _state.Queue.Count == 0)
                _state.SomeoneEating = false;

            _logger.LogInformation(
                "CoordinatorService: философ {Id} завершает работу и удален из очереди",
                philosopherId);

            if (_state.Queue.Any())
            {
                next = _state.Queue.Dequeue();
                _state.SomeoneEating = true;
            }
        }

        if (next != null)
        {
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = next
            });
        }

        if (_finishedPhilosophers == _config.PhilosophersCount)
        {
            _appLifetime.StopApplication();

        }
    }


}
