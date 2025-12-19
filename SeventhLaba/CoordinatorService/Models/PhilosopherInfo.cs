using CoordinatorService.Models.Enums;

namespace CoordinatorService.Models
{
    public class PhilosopherInfo
    {

        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public int LeftForkId { get; init; }
        public int RightForkId { get; init; }
        public PhilosopherState Status { get; set; } = PhilosopherState.Thinking;
    }
}
