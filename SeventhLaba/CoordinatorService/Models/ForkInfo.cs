namespace CoordinatorService.Models
{
    public class ForkInfo
    {
        public int ForkId { get; init; }
        public bool IsAvailable { get; set; } = true;
        public string? UsedByPhilosopherId { get; set; }
    }
}
