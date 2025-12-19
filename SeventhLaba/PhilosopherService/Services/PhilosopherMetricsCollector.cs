using PhilosopherService.Interfaces;
using PhilosopherService.Models;

namespace PhilosopherService.Services
{
    public class PhilosopherMetricsCollector : IPhilosopherMetricsCollector
    {
        private readonly PhilosopherMetrics _metrics = new PhilosopherMetrics();
        

        public void RecordEating()
        {
            _metrics._eatCount++;
        }

        public void RecordEatingTime(TimeSpan eatingTime)
        {
            _metrics._eatingTimes.Add(eatingTime);
        }

        public void RecordThinkingTime(TimeSpan thinkingTime)
        {
            _metrics._thinkingTimes.Add(thinkingTime);
        }

        public void RecordWaitingTime(TimeSpan waitingTime)
        {
            _metrics._hungryTimes.Add(waitingTime);
        }

        public void IncrementEatCount()
        {
            _metrics._eatCount++;
        }

        public void AddEatingTime(TimeSpan time)
        {
            _metrics._eatingTimes.Add(time);
        }

        public void AddThinkingTime(TimeSpan time)
        {
            _metrics._thinkingTimes.Add(time);
        }

        public void AddHungryTime(TimeSpan time)
        {
            _metrics._hungryTimes.Add(time);
        }

        public int GetEatCount()
        {
            return _metrics._eatCount;
        }

        public TimeSpan GetAverageEatingTime()
        {
            return _metrics._eatingTimes.Count > 0
                ? TimeSpan.FromTicks((long)_metrics._eatingTimes.Average(t => t.Ticks))
                : TimeSpan.Zero;
        }

        public TimeSpan GetAverageThinkingTime()
        {
            return _metrics._thinkingTimes.Count > 0
                ? TimeSpan.FromTicks((long)_metrics._thinkingTimes.Average(t => t.Ticks))
                : TimeSpan.Zero;
        }

        public TimeSpan GetAverageHungryTime()
        {
            return _metrics._hungryTimes.Count > 0
                ? TimeSpan.FromTicks((long)_metrics._hungryTimes.Average(t => t.Ticks))
                : TimeSpan.Zero;
        }

        public TimeSpan GetTotalEatingTime()
        {
            return TimeSpan.FromTicks(_metrics._eatingTimes.Sum(t => t.Ticks));
        }

        public TimeSpan GetTotalThinkingTime()
        {
            return TimeSpan.FromTicks(_metrics._thinkingTimes.Sum(t => t.Ticks));
        }

        public TimeSpan GetTotalHungryTime()
        {
            return TimeSpan.FromTicks(_metrics._hungryTimes.Sum(t => t.Ticks));
        }

        public TimeSpan GetMaximumEatingTime()
        {
            return TimeSpan.FromTicks(_metrics._eatingTimes.Max(t => t.Ticks));
        }

        public TimeSpan GetMaximumThinkingTime()
        {
            return TimeSpan.FromTicks(_metrics._thinkingTimes.Max(t => t.Ticks));
        }

        public TimeSpan GetMaximumHungryTime()
        {
            return TimeSpan.FromTicks(_metrics._hungryTimes.Max(t => t.Ticks));
        }
    }
}
