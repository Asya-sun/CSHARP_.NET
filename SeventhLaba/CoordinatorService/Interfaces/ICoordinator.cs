namespace CoordinatorService.Interfaces
{
    public interface ICoordinator
    {
        Task RequestToEatAsync(string philosopherId);
        Task FinishedEatingAsync(string philosopherId);
        Task PhilosopherExitingAsync(string philosopherId);
    }
}
