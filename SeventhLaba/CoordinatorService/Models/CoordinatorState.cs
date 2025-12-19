namespace CoordinatorService.Models
{
    public class CoordinatorState
    {
        public Dictionary<string, PhilosopherInfo> Philosophers { get; } = new();
        public Dictionary<int, ForkInfo> Forks { get; } = new();

        public Queue<string> HungryQueue { get; } = new();
        public object Lock { get; } = new();
    }
}
