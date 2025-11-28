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

    public async Task<Guid> StartNewRunAsync(SimulationOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var run = new SimulationRun
        {
            RunId = runId,
            StartedAt = DateTime.UtcNow,
            OptionsJson = JsonSerializer.Serialize(options),
        };

        context.SimulationRuns.Add(run);
        await context.SaveChangesAsync();

        _logger.LogInformation("Создан новый запуск симуляции: {RunId}", runId);
        return runId;
    }

    public async Task CompleteRunAsync(Guid runId)
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

    public async Task<List<ForkStateChange>> GetForkStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Находим последнее состояние каждой вилки до указанного времени симуляции
        var latestStates = await context.ForkStateChanges
            .Where(f => f.RunId == runId && f.SimulationTime <= simulationTime)
            .GroupBy(f => f.ForkId)
            .Select(g => g.OrderByDescending(f => f.SimulationTime).ThenByDescending(f => f.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

    public async Task<List<PhilosopherStateChange>> GetPhilosopherStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var latestStates = await context.PhilosopherStateChanges
            .Where(p => p.RunId == runId && p.SimulationTime <= simulationTime)
            .GroupBy(p => p.PhilosopherName)
            .Select(g => g.OrderByDescending(p => p.SimulationTime).ThenByDescending(p => p.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

    public async Task<SimulationRun?> GetRunAsync(Guid runId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SimulationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RunId == runId);
    }

    public async Task RecordForkStateAsync(Guid runId, int forkId, ForkState state, PhilosopherName? usedBy, TimeSpan simulationTime)
    {
        
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var change = new ForkStateChange
            {
                RunId = runId,
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
            _logger.LogError(ex, "Ошибка сохранения состояния вилки {ForkId} для RunId {RunId}", forkId, runId);
            throw;
        }
    }

    public async Task RecordPhilosopherStateAsync(Guid runId, PhilosopherName name, PhilosopherState state, string action, string strategyName, TimeSpan simulationTime)
    {

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var change = new PhilosopherStateChange
            {
                RunId = runId,
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
            _logger.LogError(ex, "Ошибка сохранения состояния философа {Philosopher} для RunId {RunId}", name, runId);
            throw;
        }
    }

    public async Task RecordDeadlockAsync(Guid runId, int deadlockNumber, TimeSpan simulationTime, PhilosopherName resolvedByPhilosopher)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var deadlock = new DeadlockRecord
            {
                RunId = runId,
                DeadlockNumber = deadlockNumber,
                DetectedAt = DateTime.UtcNow,
                SimulationTime = simulationTime,
                ResolvedByPhilosopher = resolvedByPhilosopher
            };

            context.DeadlockRecords.Add(deadlock);
            await context.SaveChangesAsync();

            _logger.LogInformation("Записан дедлок #{DeadlockNumber} для RunId {RunId}", deadlockNumber, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения дедлока для RunId {RunId}", runId);
            throw;
        }
    }

    public async Task<List<DeadlockRecord>> GetDeadlocksAsync(Guid runId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DeadlockRecords
            .Where(d => d.RunId == runId)
            .OrderBy(d => d.DeadlockNumber)
            .ToListAsync();
    }


}
