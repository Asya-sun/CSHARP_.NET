using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Metrics
{
    public class SimulationMetrics
    {
        public int TotalSteps { get; set; }
        public int TotalEatCount { get; set; }
        public double AverageThroughput => TotalSteps > 0 ? (double)TotalEatCount / TotalSteps * 1000 : 0;

        public int MaxHungerTime { get; set; }
        public string MaxHungerPhilosopher { get; set; } = string.Empty;
        public double AverageHungerTime { get; set; }

        public Dictionary<int, ForkMetrics> ForkMetrics { get; } = new();
    }

}
