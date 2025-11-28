using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;

public class ForkStateChange
{
    public int Id { get; set; }
    // внешний ключ + навигационное свойство
    public int SimulationRunId { get; set; }
    public SimulationRun SimulationRun { get; set; } = null!;

    public int ForkId { get; set; }
    public ForkState State { get; set; }
    public PhilosopherName? UsedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan SimulationTime { get; set; }
}
