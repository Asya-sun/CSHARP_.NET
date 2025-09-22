using Philosophers.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface IPhilosopherStrategy
    {
        string _name { get; }
        void Initialize(Philosopher philosopher);

        public bool TryAcquireForks();
    }
}
