using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Context;
using Philosophers.DB.Entities;
using Philosophers.DB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Philosophers.DB.Repositories;

public class SimulationRepository : ISimulationRepository
{
    private readonly ILogger<SimulationRepository> _logger;
    private readonly IDbContextFactory<SimulationDBContext> _contextFactory;

    public SimulationRepository(IDbContextFactory<SimulationDBContext> contextFactory, ILogger<SimulationRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task StartNewRunAsync(SimulationOptions options, int runId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var run = new SimulationRun
        {
            RunId = runId,
            StartedAt = DateTime.UtcNow,
            OptionsJson = JsonSerializer.Serialize(options),
        };

        context.SimulationRuns.Add(run);
        await context.SaveChangesAsync();

        _logger.LogInformation("Создан новый запуск симуляции: {RunId}", run.RunId);
        //return run.RunId;
    }

    public async Task CompleteRunAsync( int runId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var run = await context.SimulationRuns
            .FirstOrDefaultAsync(r => r.RunId == runId);

        if (run != null)
        {
            run.FinishedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }


    public async Task<SimulationRun?> GetRunAsync(int runId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SimulationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RunId == runId);
    }


    public async Task RecordPhilosopherStateAsync(int simulationRunId, PhilosopherName name, PhilosopherState state, string action, string strategyName, TimeSpan simulationTime)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var change = new PhilosopherStateChange
            {
                SimulationRunId = simulationRunId,
                PhilosopherName = name,
                State = state,
                Action = action,
                StrategyName = strategyName,
                Timestamp = DateTime.UtcNow,
                SimulationTime = simulationTime
            };

            context.PhilosopherStateChanges.Add(change);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения состояния философа {Philosopher} для SimulationRunId {SimulationRunId}", name, simulationRunId);
            throw;
        }
    }

    public async Task RecordForkStateAsync(int simulationRunId, int forkId, ForkState state, PhilosopherName? usedBy, TimeSpan simulationTime)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var change = new ForkStateChange
            {
                SimulationRunId = simulationRunId, // Только этот ID
                ForkId = forkId,
                State = state,
                UsedBy = usedBy,
                Timestamp = DateTime.UtcNow,
                SimulationTime = simulationTime
            };

            context.ForkStateChanges.Add(change);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения состояния вилки {ForkId} для SimulationRunId {SimulationRunId}", forkId, simulationRunId);
            throw;
        }
    }

    public async Task RecordDeadlockAsync(int simulationRunId, int deadlockNumber, TimeSpan simulationTime, PhilosopherName resolvedByPhilosopher)
    {   
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var deadlock = new DeadlockRecord
            {
                SimulationRunId = simulationRunId,
                DeadlockNumber = deadlockNumber,
                DetectedAt = DateTime.UtcNow,
                SimulationTime = simulationTime,
                ResolvedByPhilosopher = resolvedByPhilosopher
            };

            context.DeadlockRecords.Add(deadlock);
            await context.SaveChangesAsync();

            _logger.LogInformation("Записан дедлок #{DeadlockNumber} для SimulationRunId {SimulationRunId}", deadlockNumber, simulationRunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения дедлока для SimulationRunId {SimulationRunId}", simulationRunId);
            throw;
        }
    }
    
    
    public async Task<List<DeadlockRecord>> GetDeadlocksAsync(int simulationRunId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DeadlockRecords
            .Where(f => f.SimulationRunId == simulationRunId)
            .OrderBy(d => d.DeadlockNumber)
            .ToListAsync();
    }



    public async Task<List<ForkStateChange>> GetForkStatesAtTimeAsync(int simulationRunId, TimeSpan simulationTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var latestStates = await context.ForkStateChanges
            .Where(f => f.SimulationRunId == simulationRunId && f.SimulationTime <= simulationTime)
            .GroupBy(f => f.ForkId)
            .Select(g => g.OrderByDescending(f => f.SimulationTime).ThenByDescending(f => f.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

    public async Task<List<PhilosopherStateChange>> GetPhilosopherStatesAtTimeAsync(int simulationRunId, TimeSpan simulationTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var latestStates = await context.PhilosopherStateChanges
            .Where(p => p.SimulationRunId == simulationRunId && p.SimulationTime <= simulationTime)
            .GroupBy(p => p.PhilosopherName)
            .Select(g => g.OrderByDescending(p => p.SimulationTime).ThenByDescending(p => p.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

}
