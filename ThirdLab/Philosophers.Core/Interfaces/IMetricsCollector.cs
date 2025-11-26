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
        void RecordEating(PhilosopherName philosopherName);
        void RecordWaitingTime(PhilosopherName philosopherName, TimeSpan waitingTime);
        void RecordThinkingTime(PhilosopherName philosopherName, TimeSpan thinkingTime);
        void RecordEatingTime(PhilosopherName philosopherName, TimeSpan eatingTime);

        void RecordDeadlock();
        int GetDeadlockCount();

        // Метрики вилок в реальном времени
        void RecordForkAcquired(int forkId, PhilosopherName philosopherName);
        void RecordForkReleased(int forkId);

        void PrintMetrics();
        int GetEatCount(PhilosopherName philosopherName);

        // for tests
        IReadOnlyDictionary<PhilosopherName, int> GetEatCounts();
        IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetWaitingTimes();
        IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetThinkingTimes();
        IReadOnlyDictionary<PhilosopherName, IReadOnlyList<TimeSpan>> GetEatingTimes();
        IReadOnlyDictionary<int, TimeSpan> GetForkUsageTimes();
    }
}