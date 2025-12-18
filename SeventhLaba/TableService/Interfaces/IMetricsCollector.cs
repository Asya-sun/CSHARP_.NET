namespace TableService.Interfaces
{
    public interface IMetricsCollector
    {
        // Метрики философов
        void RecordEating(string philosopherId);
        void RecordWaitingTime(string philosopherId, TimeSpan waitingTime);
        void RecordThinkingTime(string philosopherId, TimeSpan thinkingTime);
        void RecordEatingTime(string philosopherId, TimeSpan eatingTime);

        void RecordDeadlock();
        int GetDeadlockCount();

        // Метрики вилок в реальном времени
        void RecordForkAcquired(int forkId, string philosopherId);
        void RecordForkReleased(int forkId);

        void PrintMetrics();
        int GetEatCount(string philosopherId);

        // for tests
        IReadOnlyDictionary<string, int> GetEatCounts();
        IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetWaitingTimes();
        IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetThinkingTimes();
        IReadOnlyDictionary<string, IReadOnlyList<TimeSpan>> GetEatingTimes();
        IReadOnlyDictionary<int, TimeSpan> GetForkUsageTimes();
    }
}
