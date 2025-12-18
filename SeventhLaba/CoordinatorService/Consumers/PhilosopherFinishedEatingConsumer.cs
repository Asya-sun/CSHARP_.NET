using MassTransit;
using Philosophers.Shared.Events;
using CoordinatorService.Interfaces;

namespace CoordinatorService.Consumers;

public class PhilosopherFinishedEatingConsumer
    : IConsumer<PhilosopherFinishedEating>
{
    private readonly ICoordinator _coordinator;

    public PhilosopherFinishedEatingConsumer(ICoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task Consume(ConsumeContext<PhilosopherFinishedEating> context)
    {
        await _coordinator.FinishedEatingAsync(
            context.Message.PhilosopherId);
    }
}
