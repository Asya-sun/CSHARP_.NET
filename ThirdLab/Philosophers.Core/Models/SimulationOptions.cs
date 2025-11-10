namespace Philosophers.Core.Models;

public class SimulationOptions
{
    public int DurationSeconds { get; set; } = 120;
    public int ThinkingTimeMin { get; set; } = 30;
    public int ThinkingTimeMax { get; set; } = 100;
    public int EatingTimeMin { get; set; } = 40;
    public int EatingTimeMax { get; set; } = 50;
    public int ForkAcquisitionTime { get; set; } = 20;
    public int DisplayUpdateInterval { get; set; } = 200;
}