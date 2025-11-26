using Microsoft.EntityFrameworkCore;
using Philosophers.DB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Context;

public class SimulationDBContext : DbContext
{
    public SimulationDBContext(DbContextOptions<SimulationDBContext> options)
        : base(options) { }

    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<PhilosopherStateChange> PhilosopherStateChanges => Set<PhilosopherStateChange>();
    public DbSet<ForkStateChange> ForkStateChanges => Set<ForkStateChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Можно добавить индексы для ускорения запросов
        modelBuilder.Entity<PhilosopherStateChange>()
            .HasIndex(p => p.RunId);

        modelBuilder.Entity<ForkStateChange>()
            .HasIndex(f => f.RunId);

        modelBuilder.Entity<SimulationRun>()
            .HasIndex(s => s.RunId)
            .IsUnique();
    }
}
