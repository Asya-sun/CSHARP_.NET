using Philosophers.Core.Interfaces;
using Philosophers.Core.Metrics;
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
        public Fork LeftFork { get;  }
        public Fork RightFork { get; }
        // Флаги владения вилками
        public bool IsHoldingLeftFork { get; private set; }
        public bool IsHoldingRightFork { get; private set; }
        public bool HasBothForks => IsHoldingLeftFork && IsHoldingRightFork;


        public IPhilosopherStrategy _strategy { get; set; } = null!;

        // Метрики
        public PhilosopherMetrics Metrics { get; private set; }

        
        public Philosopher(int id, string name, Fork leftFork, Fork rightFork, IPhilosopherStrategy strategy,  CancellationToken  token )
        {
            _id = id;
            _name = name;
            LeftFork = leftFork;
            RightFork = rightFork;
            _thread = new Thread(Run);
            _strategy = strategy;
            _cancellationToken = token;
            Metrics = new PhilosopherMetrics(this);
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
                        Metrics.CurrentAction = "Eating";
                        Eat();
                        ReleaseForks();

                        State = PhilosopherState.Thinking;
                        Metrics.CurrentAction = "Thinking";
                        break;
                    case PhilosopherState.Thinking:
                        Think();
                        State = PhilosopherState.Hungry;
                        _hungryTimer.Restart();
                        Metrics.CurrentAction = "TryingToAcquireForks";
                        break;
                    case PhilosopherState.Hungry:
                        if (TryEat())
                        {
                            _hungryTimer.Stop();
                            Metrics.TotalHungryTimeMs += _hungryTimer.ElapsedMilliseconds;
                            State = PhilosopherState.Eating;
                            Metrics.HungryCount++;

                            Metrics.CurrentAction = "Eating";

                        }
                        else
                        {
                            Metrics.CurrentAction = "WaitingForForks";
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
        public bool TryTakeLeftFork()
        {
            if (!IsHoldingLeftFork && LeftFork.TryTake(this))
            {
                IsHoldingLeftFork = true;
                return true;
            }
            return false;
        }

        public bool TryTakeRightFork()
        {
            if (!IsHoldingRightFork && RightFork.TryTake(this))
            {
                IsHoldingRightFork = true;
                return true;
            }
            return false;
        }

        private void Eat()
        {
            int eatTime = _random.Next(40, 51);
            Thread.Sleep(eatTime);
            Metrics.TotalMealsEaten++;
            //_mealsEaten++;
        }

        public void ReleaseLeftFork()
        {
            if (IsHoldingLeftFork)
            {
                LeftFork.Release(this);
                IsHoldingLeftFork = false;
            }
        }

        public void ReleaseRightFork()
        {
            if (IsHoldingRightFork)
            {
                RightFork.Release(this);
                IsHoldingRightFork = false;
            }
        }

        public void ReleaseForks()
        {
            ReleaseLeftFork();
            ReleaseRightFork();
        }

        public double GetAverageHungryTime() => Metrics.AverageHungryTimeMs;
        //public double GetAverageHungryTime()
        //{
        //    return Metrics.HungryCount > 0 ? (double)Metrics.TotalHungryTimeMs / Metrics.HungryCount : 0;
        //}
    }
}
