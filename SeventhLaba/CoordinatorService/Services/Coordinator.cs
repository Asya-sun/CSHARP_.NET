using CoordinatorService.Interfaces;
using CoordinatorService.Models;
using CoordinatorService.Models.Enums;
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
    private int _forkCount = 0;

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

        _forkCount = _config.PhilosophersCount == 1 ? 2 : _config.PhilosophersCount;
        for (int i = 0; i < _forkCount; i++)
        {
            //state.Forks[i] = new ForkInfo{ ForkId = i };
            if (!_state.Forks.ContainsKey(i))
                _state.Forks[i] = new ForkInfo { ForkId = i };
        }
    }

    public Task RegisterAsync(
    string id,
    string name,
    int leftFork,
    int rightFork)
    {
        lock (_state.Lock)
        {
            _state.Philosophers[id] = new PhilosopherInfo
            {
                Id = id,
                Name = name,
                LeftForkId = leftFork,
                RightForkId = rightFork,
                Status = PhilosopherState.Thinking
            };

            _logger.LogInformation(
                "Coordinator: зарегистрирован философ {Name} (ID: {Id}) с вилками L={LeftFork}, R={RightFork}. Всего философов: {Count}",
                name, id, leftFork, rightFork, _state.Philosophers.Count);

            if (!_state.Forks.ContainsKey(leftFork))
                _state.Forks[leftFork] = new ForkInfo { ForkId = leftFork };

            if (!_state.Forks.ContainsKey(rightFork))
                _state.Forks[rightFork] = new ForkInfo { ForkId = rightFork };
        }
        return Task.CompletedTask;
    }

    public async Task RequestToEatAsync(string philosopherId)
    {
        bool allowed = false;
        _logger.LogDebug(
            "Coordinator: получен запрос на еду от философа {PhilosopherId}",
            philosopherId);

        lock (_state.Lock)
        {

            var philosopher = _state.Philosophers[philosopherId];
            philosopher.Status = PhilosopherState.Hungry;

            var leftFork = _state.Forks[philosopher.LeftForkId];
            var rightFork = _state.Forks[philosopher.RightForkId];

            if (leftFork.IsAvailable && rightFork.IsAvailable)
            {
                // резервирование вилок
                leftFork.IsAvailable = false;
                rightFork.IsAvailable = false;
                leftFork.UsedByPhilosopherId = philosopherId;
                rightFork.UsedByPhilosopherId = philosopherId;

                philosopher.Status = PhilosopherState.Eating;
                allowed = true;
            }
            else
            {
                _state.HungryQueue.Enqueue(philosopherId);
                _logger.LogDebug(
                    "Coordinator: философ {PhilosopherId} добавлен в очередь голодных. Размер очереди: {QueueSize}",
                    philosopherId, _state.HungryQueue.Count);
            }
        }

        if (allowed)
        {
            _logger.LogDebug(
                "Coordinator: получен запрос на еду от философа {PhilosopherId}",
                philosopherId);
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = philosopherId
            });
        }
    }

    public async Task FinishedEatingAsync(string philosopherId)
    {
        List<string> newlyAllowed = new();
        
        
        _logger.LogDebug(
            "Coordinator: философ {PhilosopherId} закончил есть. Освобождаем вилки...",
            philosopherId);

        lock (_state.Lock)
        {
            var philosopher = _state.Philosophers[philosopherId];
            philosopher.Status = PhilosopherState.Thinking;

            var leftFork = _state.Forks[philosopher.LeftForkId];
            var rightFork = _state.Forks[philosopher.RightForkId];

            _logger.LogInformation(
                "Coordinator: вилки L={LeftFork}, R={RightFork} освобождены философом {PhilosopherId}",
                philosopher.LeftForkId, philosopher.RightForkId, philosopherId);

            leftFork.IsAvailable = true;
            rightFork.IsAvailable = true;
            leftFork.UsedByPhilosopherId = null;
            rightFork.UsedByPhilosopherId = null;

            // пробуем разбудить очередь
            int count = _state.HungryQueue.Count;

            for (int i = 0; i < count; i++)
            {
                var nextId = _state.HungryQueue.Dequeue();
                var next = _state.Philosophers[nextId];

                var lf = _state.Forks[next.LeftForkId];
                var rf = _state.Forks[next.RightForkId];

                _logger.LogDebug(
                    "Coordinator: пробуждаем философа {NextId} из очереди. Доступные вилки: L={LeftForkAvailable}, R={RightForkAvailable}",
                    nextId, lf.IsAvailable, rf.IsAvailable);
                if (lf.IsAvailable && rf.IsAvailable)
                {
                    lf.IsAvailable = false;
                    rf.IsAvailable = false;
                    lf.UsedByPhilosopherId = nextId;
                    rf.UsedByPhilosopherId = nextId;

                    next.Status = PhilosopherState.Eating;
                    newlyAllowed.Add(nextId);
                }
                else
                {
                    _state.HungryQueue.Enqueue(nextId);
                }
            }
        }

        foreach (var id in newlyAllowed)
        {
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = id
            });
        }
    }



    public async Task PhilosopherExitingAsync(string philosopherId)
    {
        List<string> newlyAllowed = new();
        var finished = Interlocked.Increment(ref _finishedPhilosophers);

        _logger.LogInformation(
            "Coordinator: получено уведомление о выходе философа {PhilosopherId}",
            philosopherId);

        lock (_state.Lock)
        {
            if (!_state.Philosophers.ContainsKey(philosopherId))
                return;

            var philosopher = _state.Philosophers[philosopherId];

            // если философ ест — освободим вилки
            if (philosopher.Status == PhilosopherState.Eating)
            {
                var leftFork = _state.Forks[philosopher.LeftForkId];
                var rightFork = _state.Forks[philosopher.RightForkId];

                leftFork.IsAvailable = true;
                rightFork.IsAvailable = true;
                leftFork.UsedByPhilosopherId = null;
                rightFork.UsedByPhilosopherId = null;
            }

            // удаляем философа из очереди голодных
            var queue = new Queue<string>(_state.HungryQueue.Where(id => id != philosopherId));
            _state.HungryQueue.Clear();
            foreach (var id in queue)
                _state.HungryQueue.Enqueue(id);

            // удаляем философа из словаря (он больше не участвует)
            _state.Philosophers.Remove(philosopherId);

            _logger.LogInformation(
                "CoordinatorService: философ {Id} завершает работу и удален",
                philosopherId);

            // пробуем разбудить очередь
            int count = _state.HungryQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var nextId = _state.HungryQueue.Dequeue();
                if (!_state.Philosophers.ContainsKey(nextId))
                    continue; // философ ушёл

                var next = _state.Philosophers[nextId];
                var lf = _state.Forks[next.LeftForkId];
                var rf = _state.Forks[next.RightForkId];

                if (lf.IsAvailable && rf.IsAvailable)
                {
                    lf.IsAvailable = false;
                    rf.IsAvailable = false;
                    lf.UsedByPhilosopherId = nextId;
                    rf.UsedByPhilosopherId = nextId;

                    next.Status = PhilosopherState.Eating;
                    newlyAllowed.Add(nextId);
                }
                else
                {
                    _state.HungryQueue.Enqueue(nextId);
                }
            }
        }

        foreach (var id in newlyAllowed)
        {
            await _publishEndpoint.Publish(new PhilosopherAllowedToEat
            {
                PhilosopherId = id
            });
        }

        // если все философы ушли — завершаем приложение
        if (_finishedPhilosophers == _config.PhilosophersCount)
        {
            _logger.LogInformation(
                "Coordinator: все философы ({FinishedCount}) завершили работу. Останавливаю приложение...",
                _finishedPhilosophers);
            _appLifetime.StopApplication();
        }
    }


}
