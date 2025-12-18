using PhilosopherService.Models;

namespace PhilosopherService.Interfaces
{
    public interface IPhilosopherService
    {
        string GetPhilosopherId();
        string GetPhilosopherName();
        void SetAllowedToEat();
    }
}
