using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Shared.DTO;
using PhilosopherService.Http;
using PhilosopherService.Interfaces;
using PhilosopherService.Models;
using System.Diagnostics;

namespace PhilosopherService.Services
{
    public class PhilosopherHostedService : BackgroundService
    {
        private readonly PhilosopherConfig _config;
        private readonly TableClient _tableClient;
        private readonly ILogger<PhilosopherHostedService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Random _random = new();

        private readonly IPhilosopherStrategy _strategy;
        private PhilosopherState _state = PhilosopherState.Thinking;
        private int _eatCount = 0;
        private int _stepsLeft = 0;
        private string _action = "None";
        private readonly Stopwatch _hungryTimer = new();
        private readonly Stopwatch _thinkingTimer = new();
        private readonly Stopwatch _eatingTimer = new();

        public PhilosopherHostedService(
            IOptions<PhilosopherConfig> config,
            TableClient tableClient,
            IPhilosopherStrategy strategy,
            ILogger<PhilosopherHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _config = config.Value;
            _tableClient = tableClient;
            _logger = logger;
            _strategy = strategy;
            _appLifetime = appLifetime;

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
                _config.PhilosopherId);

            await _tableClient.UnregisterAsync(request, ct);
        }

        private async Task PerformCycleAsync(CancellationToken cancellationToken)
        {
            switch (_state)
            {
                case PhilosopherState.Thinking:
                    await ThinkAsync(cancellationToken);
                    _thinkingTimer.Stop();

                    _state = PhilosopherState.Hungry;
                    _hungryTimer.Restart();
                    _action = "TakeLeftFork|TakeRightFork";

                    await UpdateStateInTableAsync();
                    _logger.LogDebug("Философ {Name} проголодался", _config.Name);
                    break;

                case PhilosopherState.Hungry:
                    await _strategy.AcquireForksAsync(
                    _config,
                    _tableClient,
                    cancellationToken);
                    _hungryTimer.Stop();

                    _state = PhilosopherState.Eating;
                    _action = "Eating";
                    _eatingTimer.Restart();
                    _eatCount++;

                    await UpdateStateInTableAsync();
                    _logger.LogDebug("Философ {Name} начинает есть", _config.Name);

                    await EatAsync(cancellationToken);
                    _eatingTimer.Stop();

                    await _strategy.ReleaseForksAsync(_config, _tableClient);

                    _state = PhilosopherState.Thinking;
                    _action = "ReleaseLeftFork|ReleaseRightFork";
                    _thinkingTimer.Restart();

                    await UpdateStateInTableAsync();
                    _logger.LogDebug("Философ {Name} закончил есть. Всего: {Count}",
                        _config.Name, _eatCount);

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
        }

        private async Task<bool> TryEatAsync(CancellationToken cancellationToken)
        {
            // Пытаемся взять левую вилку
            var leftRequest = new AcquireForkRequest(
                _config.PhilosopherId,
                _config.LeftForkId);

            bool leftAcquired = await _tableClient.TryAcquireForkAsync(leftRequest, cancellationToken);
            if (!leftAcquired)
            {
                _logger.LogDebug("Философ {Name} не смог взять левую вилку {ForkId}",
                    _config.Name, _config.LeftForkId);
                return false;
            }

            await Task.Delay(100, cancellationToken);

            // Пытаемся взять правую вилку
            var rightRequest = new AcquireForkRequest(
                _config.PhilosopherId,
                _config.RightForkId);

            bool rightAcquired = await _tableClient.TryAcquireForkAsync(rightRequest, cancellationToken);
            if (!rightAcquired)
            {
                _logger.LogDebug("Философ {Name} не смог взять правую вилку {ForkId}, отпускает левую",
                    _config.Name, _config.RightForkId);

                var releaseRequest = new ReleaseForkRequest(
                    _config.PhilosopherId,
                    _config.LeftForkId);
                await _tableClient.ReleaseForkAsync(releaseRequest);
                return false;
            }

            await Task.Delay(100, cancellationToken);

            _logger.LogDebug("Философ {Name} взял обе вилки", _config.Name);
            return true;
        }

        private async Task ReleaseForksAsync()
        {
            try
            {
                var leftRequest = new ReleaseForkRequest(
                    _config.PhilosopherId,
                    _config.LeftForkId);

                var rightRequest = new ReleaseForkRequest(
                    _config.PhilosopherId,
                    _config.RightForkId);

                await Task.WhenAll(
                    _tableClient.ReleaseForkAsync(leftRequest),
                    _tableClient.ReleaseForkAsync(rightRequest)
                );

                _logger.LogDebug("Философ {Name} положил обе вилки", _config.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при возврате вилок философом {Name}", _config.Name);
            }
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
    }

    public enum PhilosopherState { Thinking, Hungry, Eating }
}