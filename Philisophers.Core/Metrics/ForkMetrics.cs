using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Metrics
{
    public class ForkMetrics
    {
        public int TotalSteps { get; set; }
        public int AvailableSteps { get; set; }
        public int InUseSteps { get; set; }

        public double AvailabilityRate => TotalSteps > 0 ? (double)AvailableSteps / TotalSteps * 100 : 0;
        public double UtilizationRate => TotalSteps > 0 ? (double)InUseSteps / TotalSteps * 100 : 0;
    }
}
