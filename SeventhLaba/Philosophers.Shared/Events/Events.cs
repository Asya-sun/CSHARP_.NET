using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Shared.Events
{
    public record PhilosopherWantsToEat
    {
        public string PhilosopherId { get; init; } = "";
    }

    public record PhilosopherAllowedToEat
    {
        public string PhilosopherId { get; init; } = "";
    }

    public record PhilosopherFinishedEating
    {
        public string PhilosopherId { get; init; } = "";
    }
}
