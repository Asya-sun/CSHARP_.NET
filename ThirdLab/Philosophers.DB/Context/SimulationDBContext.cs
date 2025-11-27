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
    public DbSet<DeadlockRecord> DeadlockRecords => Set<DeadlockRecord>();

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
        
    //}
}
