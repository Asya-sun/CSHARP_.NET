using Philosophers.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Metrics
{
    public class PhilosopherMetrics
    {
        public Philosopher Philosopher { get; }
        public int TotalMealsEaten { get; set; }
        public long TotalHungryTimeMs { get; set; }
        public int HungryCount { get; set; }
        public string CurrentAction { get; set; } = "None";

        public double AverageHungryTimeMs => HungryCount > 0 ? (double)TotalHungryTimeMs / HungryCount : 0;
        public double ThroughputPerSecond { get; set; }

        public PhilosopherMetrics(Philosopher philosopher)
        {
            Philosopher = philosopher;
        }

    }
}
