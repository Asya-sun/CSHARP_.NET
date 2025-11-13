using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface IMetricsCollector
    {
        // Метрики философов
        void RecordEating(string philosopherName);
        void RecordWaitingTime(string philosopherName, TimeSpan waitingTime);
        void RecordThinkingTime(string philosopherName, TimeSpan thinkingTime);
        void RecordEatingTime(string philosopherName, TimeSpan eatingTime);

        // Метрики вилок в реальном времени
        void RecordForkAcquired(int forkId, string philosopherName);
        void RecordForkReleased(int forkId);

        void PrintMetrics();
        int GetEatCount(string philosopherName);
    }
}