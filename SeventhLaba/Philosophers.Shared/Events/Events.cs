using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Shared.Events
{
    public record PhilosopherRegistered
    {
        public string PhilosopherId { get; init; } = "";
        public string Name { get; init; } = "";
        public int LeftForkId { get; init; }
        public int RightForkId { get; init; }
    }
    public record PhilosopherWantsToEat
    {
        public string PhilosopherId { get; init; } = "";
    }

    public record PhilosopherAllowedToEat
    {
        public string PhilosopherId { get; init; } = "";
    }

    //public record PhilosopherAllowedToTakeFork
    //{
    //    public string PhilosopherId;
    //    public int ForkId;
    //}


    public record PhilosopherFinishedEating
    {
        public string PhilosopherId { get; init; } = "";
    }

    /* in case philosopher send request to end and then is going to shut down
     * needed to say coordinator that fork is no more needed
     */
    public record PhilosopherExiting
    {
        public string PhilosopherId { get; init; } = "";
    }
}
