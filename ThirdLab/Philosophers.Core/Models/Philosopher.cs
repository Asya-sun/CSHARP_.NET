using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Models;

public class Philosopher
{
    public PhilosopherName Name { get; set; }
    public PhilosopherState State { get; set; }
    public int EatCount { get; set; }
    public string Action { get; set; } = "None";

    public Philosopher(PhilosopherName name)
    {
        Name = name;
        State = PhilosopherState.Thinking;
        EatCount = 0;
    }
}