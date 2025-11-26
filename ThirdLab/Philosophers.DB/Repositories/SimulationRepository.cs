using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Context;
using Philosophers.DB.Entities;
using Philosophers.DB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Repositories;

public class SimulationRepository : ISimulationRepository
{
    private readonly SimulationDBContext _context;

    public SimulationRepository(SimulationDBContext context)
    {
        _context = context;
    }

    public Task CompleteRunAsync(Guid runId)
    {
        throw new NotImplementedException();
    }

    public Task<List<ForkStateChange>> GetForkStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        throw new NotImplementedException();
    }

    public Task<List<PhilosopherStateChange>> GetPhilosopherStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        throw new NotImplementedException();
    }

    public Task<SimulationRun?> GetRunAsync(Guid runId)
    {
        throw new NotImplementedException();
    }

    public Task RecordForkStateAsync(Guid runId, int forkId, ForkState state, PhilosopherName? usedBy, TimeSpan simulationTime)
    {
        throw new NotImplementedException();
    }

    public Task RecordPhilosopherStateAsync(Guid runId, PhilosopherName name, PhilosopherState state, string action, TimeSpan simulationTime)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> StartNewRunAsync(SimulationOptions options)
    {
        throw new NotImplementedException();
    }
}
