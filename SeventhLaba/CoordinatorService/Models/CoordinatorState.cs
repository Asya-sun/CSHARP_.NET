namespace CoordinatorService.Models
{
    public class CoordinatorState
    {
        public bool SomeoneEating { get; set; } = false;
        public Queue<string> Queue { get; } = new();
        public object Lock { get; } = new();
    }
}
