using Philosophers.Shared.DTO;

namespace TableService.Interfaces
{
    public interface ITableMetricsCollector
    {
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

        // для нового способа сбора статистики от философов

        void RecordPhilosopherMetrics(UnregisterPhilosopherRequest philosopherMetrics);
    }
}
