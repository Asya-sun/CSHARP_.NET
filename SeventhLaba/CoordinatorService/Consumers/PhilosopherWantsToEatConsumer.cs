using MassTransit;
using Philosophers.Shared.Events;
using CoordinatorService.Interfaces;

namespace CoordinatorService.Consumers;

public class PhilosopherWantsToEatConsumer
    : IConsumer<PhilosopherWantsToEat>
{
    private readonly ICoordinator _coordinator;

    public PhilosopherWantsToEatConsumer(ICoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task Consume(ConsumeContext<PhilosopherWantsToEat> context)
    {
        await _coordinator.RequestToEatAsync(
            context.Message.PhilosopherId);
    }
}
