using MassTransit;
using Microsoft.Extensions.Logging;
using Philosophers.Shared.Events;
using PhilosopherService.Interfaces;
using PhilosopherService.Models;
using PhilosopherService.Services;

namespace PhilosopherService.Consumers
{
    public class PhilosopherAllowedToEatConsumer : IConsumer<PhilosopherAllowedToEat>
    {
        private readonly IPhilosopherService _philosopherService;
        private readonly ILogger<PhilosopherAllowedToEatConsumer> _logger;

        public PhilosopherAllowedToEatConsumer(
            IPhilosopherService philosopherService,
            ILogger<PhilosopherAllowedToEatConsumer> logger)
        {
            _philosopherService = philosopherService;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PhilosopherAllowedToEat> context)
        {
            if (context.Message.PhilosopherId == _philosopherService.GetPhilosopherId())
            {
                _logger.LogDebug("Философ {Name} получил разрешение есть", _philosopherService.GetPhilosopherName());
                _philosopherService.SetAllowedToEat(); // Метод, который завершает ожидание
            }
            return Task.CompletedTask;
        }
    }
}
