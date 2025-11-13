using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Models;

public class Philosopher
{
    public string Name { get; set; }
    public PhilosopherState State { get; set; }
    public int EatCount { get; set; }
    public string Action { get; set; } = "None";

    public Philosopher(string name)
    {
        Name = name;
        State = PhilosopherState.Thinking;
        EatCount = 0;
    }
}