using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Interfaces;

public interface ITableManager
{
    // Основные методы для работы с вилками
    //Task<bool> TryAcquireForkAsync(int forkId, string philosopherName, CancellationToken cancellationToken);
    Task<bool> WaitForForkAsync(int forkId, string philosopherName, CancellationToken cancellationToken, int? timeoutMs = null);
    void ReleaseFork(int forkId, string philosopherName);

    // Методы для получения состояния
    ForkState GetForkState(int forkId);
    string? GetForkOwner(int forkId);
    (int leftForkId, int rightForkId) GetPhilosopherForks(string philosopherName);

    // Методы для отображения состояния
    IReadOnlyList<Philosopher> GetAllPhilosophers();
    IReadOnlyList<Fork> GetAllForks();
    (ForkState left, ForkState right) GetAdjacentForksState(string philosopherName);

    void UpdatePhilosopherState(string name, PhilosopherState state,string action = "None");
}