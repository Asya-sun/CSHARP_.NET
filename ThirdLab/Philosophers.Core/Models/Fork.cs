using Philosophers.Core.Models.Enums;

namespace Philosophers.Core.Models;

public class Fork
{
    public int _id { get; set; }
    public ForkState _state { get; set; }
    public string? _usedBy { get; set; }

    public Fork(int id)
    {
        _id = id;
        _state = ForkState.Available;
    }

    public Fork() { }
}