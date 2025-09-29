using Philosophers.Core.Metrics;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.ConsoleApp.Metrics
{
    public class SimulationMetrics
    {
        public long TotalSimulationTimeMs { get; set; }

        public int TotalMealsEaten => PhilosopherMetrics.Sum(p => p.TotalMealsEaten);
        public double TotalThroughputPerSecond => TotalSimulationTimeMs > 0 ? (double)TotalMealsEaten / TotalSimulationTimeMs * 1000 : 0;

        // Теперь можно использовать Count, т.к. это List
        public double AverageHungryTimeMs => PhilosopherMetrics.Count > 0 ? PhilosopherMetrics.Average(p => p.AverageHungryTimeMs) : 0;

        public PhilosopherMetrics? MaxHungryPhilosopher => PhilosopherMetrics.OrderByDescending(p => p.AverageHungryTimeMs).FirstOrDefault();

        // Используем List вместо IEnumerable для удобства
        public List<PhilosopherMetrics> PhilosopherMetrics { get; private set; } = new List<PhilosopherMetrics>();
        public List<ForkMetrics> ForkMetrics { get; private set; } = new List<ForkMetrics>();

        public void UpdateMetrics(IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks, long simulationTimeMs)
        {
            TotalSimulationTimeMs = simulationTimeMs;

            PhilosopherMetrics.Clear();
            PhilosopherMetrics.AddRange(philosophers.Select(p => p.Metrics));

            ForkMetrics.Clear();
            ForkMetrics.AddRange(forks.Select(f => f.Metrics));

            foreach (var metrics in PhilosopherMetrics)
            {
                metrics.ThroughputPerSecond = TotalSimulationTimeMs > 0 ?
                    (double)metrics.TotalMealsEaten / TotalSimulationTimeMs * 1000 : 0;
            }

        }

    }
}
