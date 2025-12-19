using CoordinatorService.Interfaces;
using MassTransit;
using Philosophers.Shared.Events;

namespace CoordinatorService.Consumers
{
    public class PhilosopherExitingConsumer : IConsumer<PhilosopherExiting>
    {
        private readonly ICoordinator _coordinator;

        public PhilosopherExitingConsumer(ICoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public async Task Consume(ConsumeContext<PhilosopherExiting> context)
        {
            await _coordinator.PhilosopherExitingAsync(context.Message.PhilosopherId);
        }
    }
}
