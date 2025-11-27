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

    public SimulationDBContext() { }

    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>(); 
    public DbSet<PhilosopherStateChange> PhilosopherStateChanges => Set<PhilosopherStateChange>();
    public DbSet<ForkStateChange> ForkStateChanges => Set<ForkStateChange>();
    public DbSet<DeadlockRecord> DeadlockRecords => Set<DeadlockRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SimulationRun
        modelBuilder.Entity<SimulationRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.OptionsJson).IsRequired().HasMaxLength(1000);

        });

        // PhilosopherStateChange
        modelBuilder.Entity<PhilosopherStateChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.PhilosopherName).IsRequired();
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StrategyName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.SimulationTime).IsRequired();

        });

        // ForkStateChange
        modelBuilder.Entity<ForkStateChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.ForkId).IsRequired();
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.SimulationTime).IsRequired();

        });

        // DeadlockRecord
        modelBuilder.Entity<DeadlockRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.DeadlockNumber).IsRequired();
            entity.Property(e => e.DetectedAt).IsRequired();
            entity.Property(e => e.SimulationTime).IsRequired();
            entity.Property(e => e.ResolvedByPhilosopher).IsRequired();

        });
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //if (!optionsBuilder.IsConfigured)
        //{
        //    // Это для миграций
        //    optionsBuilder.UseNpgsql("Host=localhost;Database=PhilosopherDB;Username=postgres;Password=postgres");
        //}
    }
}

