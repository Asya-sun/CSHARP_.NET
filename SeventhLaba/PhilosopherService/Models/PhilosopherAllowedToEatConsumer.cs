using MassTransit;
using Microsoft.Extensions.Logging;
using Philosophers.Shared.Events;
using PhilosopherService.Services;
using PhilosopherService.Models;

namespace PhilosopherService.Models
{
    public class PhilosopherAllowedToEatConsumer : IConsumer<PhilosopherAllowedToEat>
    {
        private readonly PhilosopherHostedService _philosopherService;
        private readonly ILogger<PhilosopherAllowedToEatConsumer> _logger;

        public PhilosopherAllowedToEatConsumer(
            PhilosopherHostedService philosopherService,
            ILogger<PhilosopherAllowedToEatConsumer> logger)
        {
            _philosopherService = philosopherService;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PhilosopherAllowedToEat> context)
        {
            if (context.Message.PhilosopherId == _philosopherService.Config.PhilosopherId)
            {
                _logger.LogInformation("Философ {Name} получил разрешение есть", _philosopherService.Config.Name);
                _philosopherService.SetAllowedToEat(); // Метод, который завершает ожидание
            }
            return Task.CompletedTask;
        }
    }
}
