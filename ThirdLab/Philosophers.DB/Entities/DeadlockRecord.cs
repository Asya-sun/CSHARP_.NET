using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;

public class DeadlockRecord
{
    public int Id { get; set; }
    // внешний ключ + навигационное свойство
    public int SimulationRunId { get; set; }
    public SimulationRun SimulationRun { get; set; } = null!;

    public int DeadlockNumber { get; set; }
    public DateTime DetectedAt { get; set; }
    public TimeSpan SimulationTime { get; set; }
    // Какого философа заставили отпустить вилки
    public PhilosopherName ResolvedByPhilosopher { get; set; }
}
