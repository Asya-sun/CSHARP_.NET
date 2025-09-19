using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System;

namespace Philosophers.Strategies.Strategies
{
    public class NaiveStrategy : IPhilosopherStrategy
    {
        public string Name => "Naive";

        private Philosopher _philosopher = null!;

        public void Initialize(Philosopher philosopher)
        {
            _philosopher = philosopher;
        }

        public void ExecuteStep()
        {
            if (_philosopher._state != PhilosopherState.Hungry)
                return;

            // пытаемся взять вилку
            TryStartTakingForks();
        }

        private void TryStartTakingForks()
        {
            // если философ уже в процессе взятия вилки - стратегия сейчас ни при чем
            if (_philosopher.TakingLeftFork || _philosopher.TakingRightFork)
            {
                return;
            }


            // Обработка взятия левой вилки
            // если философ не пытается сейчас взять вилку и ее у него нет - то пытается ее взять
            // если получилось - выходим из стратегии - она сделала свое дело
            // иначе - пытаемся взять другую вилку
            if (!_philosopher.HasLeftFork)
            {
                
                bool gotFork = _philosopher.TryTakeLeftFork();
                if (gotFork) return;
            }

            // если мы тут - значит, левая вилка либо есть, либо взять ее не получилось
            if (!_philosopher.HasRightFork )
            {

                bool gotFork = _philosopher.TryTakeRightFork();
                if (gotFork) return;
            }
        }
    }
}