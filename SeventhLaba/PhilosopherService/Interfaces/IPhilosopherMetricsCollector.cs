namespace PhilosopherService.Interfaces
{
    public interface IPhilosopherMetricsCollector
    {
        void RecordEating();
        void RecordWaitingTime(TimeSpan waitingTime);
        void RecordThinkingTime(TimeSpan thinkingTime);
        void RecordEatingTime(TimeSpan eatingTime);

        void IncrementEatCount();

        public void AddEatingTime(TimeSpan time);

        public void AddThinkingTime(TimeSpan time);
        

        public void AddHungryTime(TimeSpan time);

        public int GetEatCount();
        public TimeSpan GetAverageEatingTime();
        public TimeSpan GetAverageThinkingTime();
        public TimeSpan GetAverageHungryTime();

        public TimeSpan GetTotalEatingTime();

        public TimeSpan GetTotalThinkingTime();

        public TimeSpan GetTotalHungryTime();
        public TimeSpan GetMaximumEatingTime();

        public TimeSpan GetMaximumThinkingTime();
        public TimeSpan GetMaximumHungryTime();

    }
}
