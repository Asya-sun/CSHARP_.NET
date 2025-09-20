using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;

namespace Philosophers.Strategies
{
    public class NaiveStrategy : IPhilosopherStrategy
    {
        public string _name => "Naive";
        private Philosopher _philosopher = null!;

        public void Initialize(Philosopher philosopher)
        {
            _philosopher = philosopher;
        }
    }
}
