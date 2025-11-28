using Microsoft.EntityFrameworkCore;
using Philosophers.DB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Context;


/*
 * контекст данных, используемый для взаимодействия с базой данных
 */
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
            entity.HasKey(e => e.RunId);

            // Связь один-ко-многим с PhilosopherStateChanges
            entity.HasMany(e => e.PhilosopherStateChanges)
                .WithOne(e => e.SimulationRun)
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь один-ко-многим с ForkStateChanges
            entity.HasMany(e => e.ForkStateChanges)
                .WithOne(e => e.SimulationRun)
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь один-ко-многим с DeadlockRecords
            entity.HasMany(e => e.DeadlockRecords)
                .WithOne(e => e.SimulationRun)
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PhilosopherStateChange
        modelBuilder.Entity<PhilosopherStateChange>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Внешний ключ обязательный (убери nullable)
            entity.Property(e => e.SimulationRunId).IsRequired();

        });

        // ForkStateChange
        modelBuilder.Entity<ForkStateChange>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Внешний ключ обязательный
            entity.Property(e => e.SimulationRunId).IsRequired();

        });

        // DeadlockRecord
        modelBuilder.Entity<DeadlockRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Внешний ключ обязательный
            entity.Property(e => e.SimulationRunId).IsRequired();

        });
    }


}

