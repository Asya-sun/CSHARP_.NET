using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Interfaces;

public interface ITableManager
{
    Task<bool> WaitForForkAsync(int forkId, PhilosopherName philosopherName, CancellationToken cancellationToken, int? timeoutMs = null);
    void ReleaseFork(int forkId, PhilosopherName philosopherName);

    // Методы для получения состояния
    ForkState GetForkState(int forkId);
    PhilosopherName? GetForkOwner(int forkId);
    (int leftForkId, int rightForkId) GetPhilosopherForks(PhilosopherName philosopherName);

    // Методы для отображения состояния
    IReadOnlyList<Philosopher> GetAllPhilosophers();
    IReadOnlyList<Fork> GetAllForks();
    (ForkState left, ForkState right) GetAdjacentForksState(PhilosopherName philosopherName);

    void UpdatePhilosopherState(PhilosopherName name, PhilosopherState state,string action = "None");
}