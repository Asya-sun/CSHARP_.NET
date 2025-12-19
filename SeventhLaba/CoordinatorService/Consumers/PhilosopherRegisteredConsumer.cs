using CoordinatorService.Interfaces;
using MassTransit;
using Philosophers.Shared.Events;

namespace CoordinatorService.Consumers
{
    public class PhilosopherRegisteredConsumer
    : IConsumer<PhilosopherRegistered>
    {
        private readonly ICoordinator _coordinator;

        public PhilosopherRegisteredConsumer(ICoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public async Task Consume(ConsumeContext<PhilosopherRegistered> context)
        {
            await _coordinator.RegisterAsync(
                context.Message.PhilosopherId,
                context.Message.Name,
                context.Message.LeftForkId,
                context.Message.RightForkId);
        }
    }

}
