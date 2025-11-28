using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;

public class PhilosopherStateChange
{
    public int Id { get; set; }
    // внешний ключ + навигационное свойство
    public int SimulationRunId { get; set; }
    public SimulationRun SimulationRun { get; set; } = null!;
    public PhilosopherName PhilosopherName { get; set; }
    public PhilosopherState State { get; set; }
    public string Action { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    // время от начала симуляции 
    public TimeSpan SimulationTime { get; set; }

}
