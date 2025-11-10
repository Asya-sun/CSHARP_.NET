using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface IMetricsCollector
    {
        
        void RecordEating(string philosopherName);
        void RecordWaitingTime(string philosopherName, TimeSpan waitingTime);
        void RecordForkUsage(int forkId, TimeSpan usageTime);
        void PrintMetrics();

    }
}
