using Philosophers.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Metrics
{
    public class ForkMetrics
    {
        
        public string ForkName { get; }
        public long TotalInUseTimeMs { get; set; }
        public long TotalAvailableTimeMs { get; set; }
        public DateTime _lastStateChange { get; set; }

        public ForkMetrics(string forkName)
        {
            ForkName = forkName;
        }

        public double GetUtilizationPercentage(long totalSimulationTimeMs)
        {
            if (totalSimulationTimeMs == 0) return 0;
            var actualInUseTime = Math.Min(TotalInUseTimeMs, totalSimulationTimeMs);
            return (double)actualInUseTime / totalSimulationTimeMs * 100;
        }
        
    }
}
