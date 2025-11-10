using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System.Diagnostics;
using System.Text;

namespace Philosophers.Services;

public abstract class PhilosopherHostedService : BackgroundService
{

    protected readonly string _name;
    protected readonly ITableManager _tableManager;
    protected readonly IPhilosopherStrategy _strategy;
    protected readonly IMetricsCollector _metricsCollector;
    protected readonly SimulationOptions _options;
    protected readonly ILogger<PhilosopherHostedService> _logger;
    protected readonly Random _random = new Random();

    protected PhilosopherState _state = PhilosopherState.Thinking;
    protected int _stepsLeft = 0;
    protected int _eatCount = 0;
    protected string _action = "None";
    protected Stopwatch _hungryTimer = new Stopwatch();

    protected PhilosopherHostedService(
        string name,
        ITableManager tableManager,
        IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        ILogger<PhilosopherHostedService> logger)
    {
        _name = name;
        _tableManager = tableManager;
        _strategy = strategy;
        _metricsCollector = metricsCollector;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Философ {Philosopher} начинает свою деятельность", _name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                switch (_state)
                {
                    case PhilosopherState.Thinking:
                        await Think(stoppingToken);
                        _state = PhilosopherState.Hungry;
                        _hungryTimer.Restart();
                        _action = "TakeLeftFork|TakeRightFork";
                        _logger.LogInformation("Философ {Philosopher} проголодался", _name);
                        break;

                    case PhilosopherState.Hungry:
                        if (await TryEat(stoppingToken))
                        {
                            _hungryTimer.Stop();
                            _metricsCollector.RecordWaitingTime(_name, _hungryTimer.Elapsed);
                            _state = PhilosopherState.Eating;
                            _stepsLeft = _random.Next(_options.EatingTimeMin, _options.EatingTimeMax + 1);
                            _action = "Eating";
                            _logger.LogInformation("Философ {Philosopher} начинает есть", _name);
                        }
                        else
                        {
                            _action = "WaitingForForks";
                            // Короткая пауза перед следующей попыткой
                            await Task.Delay(10, stoppingToken);
                        }
                        break;

                    case PhilosopherState.Eating:
                        await Eat(stoppingToken);
                        _strategy.ReleaseForks(_name, _tableManager);
                        _eatCount++;
                        _metricsCollector.RecordEating(_name);
                        _state = PhilosopherState.Thinking;
                        _action = "ReleaseLeftFork|ReleaseRightFork";
                        _logger.LogInformation("Философ {Philosopher} закончил есть. Всего поел: {EatCount} раз", _name, _eatCount);
                        break;
                }

                // Обновляем состояние в TableManager
                UpdateStateInTableManager();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в философе {Philosopher}", _name);
            }
        }

        _logger.LogInformation("Философ {Philosopher} завершает свою деятельность", _name);
    }

    protected virtual async Task Think(CancellationToken cancellationToken)
    {
        int thinkTime = _random.Next(_options.ThinkingTimeMin, _options.ThinkingTimeMax + 1);
        _stepsLeft = thinkTime;

        while (_stepsLeft > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken); // Имитация шага мышления
            _stepsLeft -= 100;
            UpdateStateInTableManager();
        }
    }

    protected virtual async Task<bool> TryEat(CancellationToken cancellationToken)
    {
        return await _strategy.TryAcquireForksAsync(_name, _tableManager, cancellationToken);
    }

    protected virtual async Task Eat(CancellationToken cancellationToken)
    {
        int eatTime = _random.Next(_options.EatingTimeMin, _options.EatingTimeMax + 1);
        _stepsLeft = eatTime;

        while (_stepsLeft > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken); // Имитация шага еды
            _stepsLeft -= 100;
            UpdateStateInTableManager();
        }
    }

    protected virtual void UpdateStateInTableManager()
    {
        // Этот метод будет реализован после доработки TableManager
        // Пока что оставляем заглушку
    }
}