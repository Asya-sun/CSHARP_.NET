using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Models
{
    public class Philosopher
    {
        private readonly Thread _thread;
        private readonly Random _random = new Random();
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _hungryTimer = new Stopwatch();
        public string _name { get; set; } = string.Empty;
        // утту должно быть set 1 раз только
        public int _id {  get; set; }
        public PhilosopherState State { get; private set; } = PhilosopherState.Thinking;
        public int _mealsEaten { get; private set; } = 0;
        public Fork LeftFork { get;  }
        public Fork RightFork { get; }

        public IPhilosopherStrategy _strategy { get; set; } = null!;

        // Метрики
        public long TotalHungryTimeMs { get; private set; }
        public int HungryCount { get; private set; }
        public string CurrentAction { get; private set; } = "None";



        public Philosopher(int id, string name, Fork leftFork, Fork rightFork, IPhilosopherStrategy strategy,  CancellationToken  token )
        {
            _id = id;
            _name = name;
            LeftFork = leftFork;
            RightFork = rightFork;
            _thread = new Thread(Run);
            _strategy = strategy;
            _cancellationToken = token;
        }

        
        public void Start() => _thread.Start();
        public void Join() => _thread.Join();


        private void Run()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                // если тут будут async методы - то нужен try catch
                switch (State)
                {
                    case PhilosopherState.Eating:
                        CurrentAction = "Eating";
                        Eat();
                        ReleaseForks();

                        State = PhilosopherState.Thinking;
                        CurrentAction = "Thinking";
                        break;
                    case PhilosopherState.Thinking:
                        Think();
                        State = PhilosopherState.Hungry;
                        _hungryTimer.Restart();
                        CurrentAction = "TryingToAcquireForks";
                        break;
                    case PhilosopherState.Hungry:
                        if (TryEat())
                        {
                            _hungryTimer.Stop();
                            TotalHungryTimeMs += _hungryTimer.ElapsedMilliseconds;
                            State = PhilosopherState.Eating;


                            CurrentAction = "Eating";

                        }
                        else
                        {
                            CurrentAction = "WaitingForForks";
                        }
                        break;
                }
            }

        }

        private bool TryEat()
        {
            if (_strategy.requestOnEat(this))
            {
                return true;
            }
            return false;
        }




        private void Think()
        {
            int thinkTime = _random.Next(30, 101);
            Thread.Sleep(thinkTime);
        }

        private void Eat()
        {
            int eatTime = _random.Next(40, 51);
            Thread.Sleep(eatTime);
            _mealsEaten++;
        }

        public void ReleaseForks()
        {
            LeftFork.Release(this);
            RightFork.Release(this);
        }


        public double GetAverageHungryTime()
        {
            return HungryCount > 0 ? (double)TotalHungryTimeMs / HungryCount : 0;
        }
    }
}
