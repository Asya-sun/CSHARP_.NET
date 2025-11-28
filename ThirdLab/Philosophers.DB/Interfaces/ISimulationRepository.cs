using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Interfaces;

// интерфейс для работы с бд
public interface ISimulationRepository
{
    // запись данных
    Task<Guid> StartNewRunAsync(SimulationOptions options);
    Task RecordPhilosopherStateAsync(Guid runId, PhilosopherName name, PhilosopherState state, string action, string strategyName, TimeSpan simulationTime);
    Task RecordForkStateAsync(Guid runId, int forkId, ForkState state, PhilosopherName? usedBy, TimeSpan simulationTime);
    Task CompleteRunAsync(Guid runId);
    Task RecordDeadlockAsync(Guid runId, int deadlockNumber, TimeSpan simulationTime, PhilosopherName resolvedByPhilosopher);


    // чтение данных
    Task<SimulationRun?> GetRunAsync(Guid runId);
    Task<List<PhilosopherStateChange>> GetPhilosopherStatesAtTimeAsync(Guid runId, TimeSpan simulationTime);
    Task<List<ForkStateChange>> GetForkStatesAtTimeAsync(Guid runId, TimeSpan simulationTime);
    Task<List<DeadlockRecord>> GetDeadlocksAsync(Guid runId);
}
