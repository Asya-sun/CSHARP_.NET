using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Shared.DTO;
using Philosophers.Shared.Events;
using PhilosopherService.Http;
using PhilosopherService.Interfaces;
using PhilosopherService.Models;
using System.Diagnostics;

namespace PhilosopherService.Services
{
    public class PhilosopherHostedService : BackgroundService, IPhilosopherService
    {
        private readonly PhilosopherConfig _config;
        private readonly TableClient _tableClient;
        private readonly ILogger<PhilosopherHostedService> _logger;
        private readonly IPhilosopherMetricsCollector _metricsCollector;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IBus _bus;

        private readonly Random _random = new();

        private readonly IPhilosopherStrategy _strategy;
        private PhilosopherState _state = PhilosopherState.Thinking;
        private int _eatCount = 0;
        private int _stepsLeft = 0;
        private string _action = "None";
        private readonly Stopwatch _hungryTimer = new();
        private readonly Stopwatch _thinkingTimer = new();
        private readonly Stopwatch _eatingTimer = new();

        public PhilosopherConfig Config => _config;



        // Для ожидания разрешения от координатора
        private TaskCompletionSource<bool>? _allowedToEatTcs;
        public void SetAllowedToEat()
        {
            _allowedToEatTcs?.TrySetResult(true);
        }

        public PhilosopherHostedService(
            IOptions<PhilosopherConfig> config,
            TableClient tableClient,
            IPhilosopherStrategy strategy,
            ILogger<PhilosopherHostedService> logger,
            IHostApplicationLifetime appLifetime,
            IPhilosopherMetricsCollector metricsCollector,
            IBus bus)
        {
            _bus = bus;

            _config = config.Value;
            _tableClient = tableClient;
            _logger = logger;
            _strategy = strategy;
            _appLifetime = appLifetime;
            _metricsCollector = metricsCollector;

            _thinkingTimer.Start();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                "Философ {Name} ({Id}) запущен на {Minutes} минут. Вилки: L={Left}, R={Right}",
                _config.Name, _config.PhilosopherId, _config.SimulationDurationMinutes,
                _config.LeftForkId, _config.RightForkId);

                // Регистрация в Coordinator
                await RegisterWithCoordinator(stoppingToken);
                // Регистрация в TableService
                await RegisterWithTableAsync(stoppingToken);


                // Основной цикл 
                var startTime = DateTime.UtcNow;
                var endTime = startTime.AddMinutes(_config.SimulationDurationMinutes);

                while (DateTime.UtcNow < endTime && !stoppingToken.IsCancellationRequested)
                {
                
                        await PerformCycleAsync(stoppingToken);
               
                }

                _logger.LogInformation(
                    "Философ {Name} завершил работу. Всего поел: {Count} раз",
                    _config.Name, _eatCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Философ {Name} остановлен (Ctrl+C)", _config.Name);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogCritical(ex,
                    "TableService недоступен. Философ {Name} завершает работу",
                    _config.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Критическая ошибка философа {Name}",
                    _config.Name);
            }
            finally
            {
                await _bus.Publish(new PhilosopherExiting { PhilosopherId = _config.PhilosopherId });
                await UnregisterFromTableAsync();
                _appLifetime.StopApplication();
            }
        }

        private async Task UnregisterFromTableAsync()
        {
            try
            {
                // тут ограничили время на отправку запроса - 5 секунд
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await UnregisterWithTableAsync(cts.Token);
                _logger.LogInformation(
                    "Философ {Name} сообщил столу о завершении",
                    _config.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Не удалось отправить unregister философа {Name}",
                    _config.Name);
            }
        }

        private async Task RegisterWithCoordinator(CancellationToken ct)
        {
            var request = new PhilosopherRegistered
            {
                PhilosopherId = _config.PhilosopherId,
                Name = _config.Name,
                LeftForkId = _config.LeftForkId,
                RightForkId = _config.RightForkId
            };
            await _bus.Publish(request);
        }
        private async Task RegisterWithTableAsync(CancellationToken ct)
        {
            var request = new RegisterPhilosopherRequest(
                _config.PhilosopherId,
                _config.Name,
                _config.LeftForkId,
                _config.RightForkId);

            await _tableClient.RegisterAsync(request, ct);
        }

        private async Task UnregisterWithTableAsync(CancellationToken ct)
        {
            var request = new UnregisterPhilosopherRequest(
                PhilosopherId: _config.PhilosopherId,
                EatCount : _metricsCollector.GetEatCount(),
                AverageHungryTime: _metricsCollector.GetAverageHungryTime(),
                AverageEatingTime: _metricsCollector.GetAverageEatingTime(),
                AverageThinkingTime: _metricsCollector.GetAverageThinkingTime(),
                TotalHungryTime: _metricsCollector.GetTotalHungryTime(),
                TotalEatingTime: _metricsCollector.GetTotalEatingTime(),
                TotalThinkingTime: _metricsCollector.GetTotalThinkingTime(),
                MaximumHungryTime: _metricsCollector.GetMaximumHungryTime(),
                MaximumEatingTime: _metricsCollector.GetMaximumEatingTime(),
                MaximumThinkingTime : _metricsCollector.GetMaximumThinkingTime());

            await _tableClient.UnregisterAsync(request, ct);
        }

        //private async Task PerformCycleAsync(CancellationToken cancellationToken)
        //{
        //    switch (_state)
        //    {
        //        case PhilosopherState.Thinking:
        //            await ThinkAsync(cancellationToken);
        //            _thinkingTimer.Stop();

        //            _state = PhilosopherState.Hungry;
        //            _hungryTimer.Restart();
        //            _action = "TakeLeftFork|TakeRightFork";

        //            await UpdateStateInTableAsync();
        //            _logger.LogDebug("Философ {Name} проголодался", _config.Name);
        //            break;

        //        case PhilosopherState.Hungry:
        //            // подготовим TCS до публикации (чтобы не пропустить быстрый ответ)
        //            _allowedToEatTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        //            // отправляем событие координатору
        //            await _bus.Publish(new PhilosopherWantsToEat { PhilosopherId = _config.PhilosopherId });
        //            _logger.LogDebug("Философ {Name} отправил WantsToEat", _config.Name);

        //            // ждём разрешения или отмены
        //            try
        //            {
        //                await _allowedToEatTcs.Task.WaitAsync(cancellationToken);
        //            }
        //            catch (OperationCanceledException)
        //            {
        //                _logger.LogDebug("Ожидание разрешения прервано для {Name}", _config.Name);
        //                return;
        //            }



        //            await _strategy.AcquireForksAsync(
        //            _config,
        //            _tableClient,
        //            cancellationToken);
        //            _hungryTimer.Stop();

        //            _state = PhilosopherState.Eating;
        //            _action = "Eating";
        //            _eatingTimer.Restart();
        //            _eatCount++;

        //            await UpdateStateInTableAsync();
        //            _logger.LogDebug("Философ {Name} начинает есть", _config.Name);

        //            await EatAsync(cancellationToken);
        //            _eatingTimer.Stop();

        //            await _strategy.ReleaseForksAsync(_config, _tableClient);
        //            // сообщаем координатору что поели
        //            await _bus.Publish(new PhilosopherFinishedEating { PhilosopherId = _config.PhilosopherId });

        //            _state = PhilosopherState.Thinking;
        //            _action = "ReleaseLeftFork|ReleaseRightFork";
        //            _thinkingTimer.Restart();

        //            await UpdateStateInTableAsync();
        //            _logger.LogDebug("Философ {Name} закончил есть. Всего: {Count}",
        //                _config.Name, _eatCount);

        //            break;
        //    }
        //}

        private async Task PerformCycleAsync(CancellationToken cancellationToken)
        {
            switch (_state)
            {
                case PhilosopherState.Thinking:
                    await ThinkAsync(cancellationToken);
                    _thinkingTimer.Stop();

                    _state = PhilosopherState.Hungry;
                    _hungryTimer.Restart();
                    _logger.LogDebug(
                        "Философ {Name} ({Id}) перешел в состояние Hungry. Ожидание вилок: L={LeftFork}, R={RightFork}",
                        _config.Name, _config.PhilosopherId, _config.LeftForkId, _config.RightForkId);
                    _action = "TakeLeftFork|TakeRightFork";

                    await UpdateStateInTableAsync();
                    _logger.LogDebug("Философ {Name} проголодался", _config.Name);
                    break;

                case PhilosopherState.Hungry:
                    _allowedToEatTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                    // Отправляем координатору запрос на еду
                    await _bus.Publish(new PhilosopherWantsToEat { PhilosopherId = _config.PhilosopherId });
                    _logger.LogDebug("Философ {Name} отправил WantsToEat", _config.Name);

                    bool gotPermission = false;
                    try
                    {
                        _logger.LogDebug(
                            "Философ {Name} ожидает разрешения от координатора...",
                            _config.Name);
                        await _allowedToEatTcs.Task.WaitAsync(cancellationToken);
                        
                        gotPermission = true;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Философ {Name} не получил разрешения есть — отмена\nУ философа {Name} сработал cancellationToken, но он не успел получить вилку.\n Шлем координатору, что вилка нам больше не понадобится", _config.Name, _config.Name);

                        _hungryTimer.Stop();
                        var hungryDuration = _hungryTimer.Elapsed;
                        _metricsCollector.RecordWaitingTime(hungryDuration);

                        return;
                    }

                    if (gotPermission)
                    {
                        _logger.LogInformation(
                            "Философ {Name} получил разрешение на еду от координатора",
                            _config.Name);

                        _hungryTimer.Stop();
                        var hungryDuration = _hungryTimer.Elapsed;
                        _metricsCollector.RecordWaitingTime(hungryDuration);

                        await _strategy.AcquireForksAsync(
                                    _config,
                                    _tableClient,
                                    cancellationToken);
                        _hungryTimer.Stop();
                        _logger.LogDebug(
                            "Философ {Name} успешно захватил обе вилки. Переходит в состояние Eating",
                            _config.Name);


                        _state = PhilosopherState.Eating;
                        _action = "Eating";
                        _eatingTimer.Restart();
                        _eatCount++;

                        await UpdateStateInTableAsync();
                        _logger.LogDebug("Философ {Name} начинает есть", _config.Name);

                        try
                        {
                            await EatAsync(cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("У философа {Name} сработал cancellationToken и он прервал еду", _config.Name);
                        }
                        finally
                        {
                            try
                            {
                                await _strategy.ReleaseForksAsync(_config, _tableClient);
                                await _bus.Publish(new PhilosopherFinishedEating { PhilosopherId = _config.PhilosopherId });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при освобождении вилок или уведомлении координатора для {Name}", _config.Name);
                            }


                            _state = PhilosopherState.Thinking;
                            _action = "ReleaseLeftFork|ReleaseRightFork";
                            _thinkingTimer.Restart();
                            await UpdateStateInTableAsync();
                            _logger.LogDebug("Философ {Name} закончил есть. Всего: {Count}", _config.Name, _eatCount);
                        }
                    }

                    break;

            }
        }


        private async Task ThinkAsync(CancellationToken cancellationToken)
        {
            int thinkTime = _random.Next(1000, 3000); // 1-3 секунды
            _stepsLeft = thinkTime;

            while (_stepsLeft > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
                _stepsLeft -= 100;
            }

            _thinkingTimer.Stop();
            var thinkingDuration = _thinkingTimer.Elapsed;
            _metricsCollector.RecordThinkingTime(thinkingDuration);
        }


        private async Task EatAsync(CancellationToken cancellationToken)
        {
            int eatTime = _random.Next(1000, 2000);
            _stepsLeft = eatTime;

            while (_stepsLeft > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
                _stepsLeft -= 100;
            }

            _eatingTimer.Stop();
            var eatingDuration = _eatingTimer.Elapsed;
            _metricsCollector.RecordEating();
            _metricsCollector.RecordEatingTime(eatingDuration);
        }

        private async Task UpdateStateInTableAsync()
        {
            try
            {
                var request = new PhilosopherStateUpdate(
                    _config.PhilosopherId,
                    _state.ToString(),
                    _action,
                    _stepsLeft,
                    _eatCount);

                 await _tableClient.UpdateStateAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления состояния философа {Name}", _config.Name);
            }
        }

        public string GetPhilosopherId()
        {
            return _config.PhilosopherId;
        }

        public string GetPhilosopherName()
        {
            return _config.Name;
        }
    }

    public enum PhilosopherState { Thinking, Hungry, Eating }
}