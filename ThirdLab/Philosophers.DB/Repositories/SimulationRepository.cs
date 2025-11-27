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
    private readonly SimulationDBContext _context;
    private readonly ILogger<SimulationRepository> _logger;

    public SimulationRepository(SimulationDBContext context, ILogger<SimulationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<Guid> StartNewRunAsync(SimulationOptions options)
    {
        var runId = Guid.NewGuid();
        var run = new SimulationRun
        {
            RunId = runId,
            StartedAt = DateTime.UtcNow,
            OptionsJson = JsonSerializer.Serialize(options),
        };

        _context.SimulationRuns.Add(run);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Создан новый запуск симуляции: {RunId}");
        return runId;
    }

    public async Task CompleteRunAsync(Guid runId)
    {
        var run = await _context.SimulationRuns
            .FirstOrDefaultAsync(r => r.RunId == runId);

        if (run != null)
        {
            run.FinishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ForkStateChange>> GetForkStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        // Находим последнее состояние каждой вилки до указанного времени симуляции
        var latestStates = await _context.ForkStateChanges
            .Where(f => f.RunId == runId && f.SimulationTime <= simulationTime)
            .GroupBy(f => f.ForkId)
            .Select(g => g.OrderByDescending(f => f.SimulationTime).ThenByDescending(f => f.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

    public async Task<List<PhilosopherStateChange>> GetPhilosopherStatesAtTimeAsync(Guid runId, TimeSpan simulationTime)
    {
        var latestStates = await _context.PhilosopherStateChanges
            .Where(p => p.RunId == runId && p.SimulationTime <= simulationTime)
            .GroupBy(p => p.PhilosopherName)
            .Select(g => g.OrderByDescending(p => p.SimulationTime).ThenByDescending(p => p.Timestamp).First())
            .ToListAsync();

        return latestStates;
    }

    public async Task<SimulationRun?> GetRunAsync(Guid runId)
    {
        return await _context.SimulationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RunId == runId);
    }

    public async Task RecordForkStateAsync(Guid runId, int forkId, ForkState state, PhilosopherName? usedBy, TimeSpan simulationTime)
    {
        try
        {
            var change = new ForkStateChange
            {
                RunId = runId,
                ForkId = forkId,
                State = state,
                UsedBy = usedBy,
                Timestamp = DateTime.UtcNow,
                SimulationTime = simulationTime
            };

            _context.ForkStateChanges.Add(change);
            await _context.SaveChangesAsync();
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
            var change = new PhilosopherStateChange
            {
                RunId = runId,
                PhilosopherName = name,
                State = state,
                Action = action,
                StrategyName = strategyName, // ← ДОБАВИЛИ!
                Timestamp = DateTime.UtcNow,
                SimulationTime = simulationTime
            };

            _context.PhilosopherStateChanges.Add(change);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения состояния философа {Philosopher} для RunId {RunId}", name, runId);
            throw;
        }
    }

}
