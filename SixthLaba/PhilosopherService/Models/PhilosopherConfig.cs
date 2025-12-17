namespace PhilosopherService.Models
{
    public class PhilosopherConfig
    {
        public string PhilosopherId { get; init; } = "local-philosopher";
        public string Name { get; init; } = "Local";
        public int LeftForkId { get; init; }
        public int RightForkId { get; init; }
        public string TableServiceUrl { get; init; } = "http://localhost:5178";
        public int SimulationDurationMinutes { get; init; }
        public string Strategy { get; init; } = "polite";
    }
}
