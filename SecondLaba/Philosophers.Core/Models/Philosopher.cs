using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Models
{
    public class Philosopher
    {
        private readonly Thread _thread;
        private readonly Random _random = new Random();

        public string _name { get; set; } = string.Empty;
        // утту должно быть set 1 раз только
        public int _id {  get; set; }
        public PhilosopherState State { get; private set; } = PhilosopherState.Thinking;
        //public int MealsEaten { get; private set; }
        public Fork LeftFork { get;  }
        public Fork RightFork { get; }
        public IPhilosopherStrategy _strategy { get; set; } = null!;



        public Philosopher(int id, string name, Fork leftFork, Fork rightFork, IPhilosopherStrategy strategy)
        {
            _id = id;
            _name = name;
            LeftFork = leftFork;
            RightFork = rightFork;
            _thread = new Thread(Run);
            _strategy = strategy;
        }

        
        public void Start() => _thread.Start();
        public void Join() => _thread.Join();


        private void Run()
        {

        }

    }
}
