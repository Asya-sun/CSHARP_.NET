using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Models;

public class Philosopher
{
    public string Name { get; set; }
    public PhilosopherState State { get; set; }
    public int StepsLeft { get; set; }
    public int EatCount { get; set; }
    public string Action { get; set; } = "None";

    public Philosopher(string name)
    {
        Name = name;
        State = PhilosopherState.Thinking;
        StepsLeft = 0;
        EatCount = 0;
    }
}