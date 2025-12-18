using TableService.Models;
using TableService.Models.Enums;

namespace TableService.Interfaces
{
    public interface ITableManager
    {
        bool RegisterPhilosopher(string philosopherId, string name, int leftForkId, int rightForkId);
        void UnregisterPhilosopher(string philosopherId);

        Task<bool> WaitForForkAsync(int forkId, string philosopherId, CancellationToken cancellationToken);
        void ReleaseFork(int forkId, string philosopherId);

        ForkState GetForkState(int forkId);
        string? GetForkOwner(int forkId);
        (int leftForkId, int rightForkId) GetPhilosopherForks(string philosopherId);

        IReadOnlyList<Philosopher> GetAllPhilosophers();
        IReadOnlyList<Fork> GetAllForks();
        (ForkState left, ForkState right) GetAdjacentForksState(string philosopherId);

        void UpdatePhilosopherState(string philosopherId, PhilosopherState state, string action = "None");
    }
}