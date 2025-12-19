namespace PhilosopherService.Models
{
    public class PhilosopherMetrics
    {
        public int _eatCount = 0;
        public readonly List<TimeSpan> _eatingTimes = new List<TimeSpan>();
        public readonly List<TimeSpan> _thinkingTimes = new List<TimeSpan>();
        public readonly List<TimeSpan> _hungryTimes = new List<TimeSpan>();
        
    }
}
