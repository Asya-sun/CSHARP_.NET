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
        private readonly Random _random = new Random();


        public bool requestOnEat(Philosopher philosopher)
        {
            // Попытка взять левую вилку
            if (!philosopher.IsHoldingLeftFork)
            {
                if (!philosopher.TryTakeLeftFork())
                {
                    return false;
                }
            }

            // Попытка взять правую вилку с таймаутом
            if (!philosopher.IsHoldingRightFork)
            {
                int attempts = 0;
                const int maxAttempts = 3;

                while (attempts < maxAttempts)
                {
                    if (philosopher.TryTakeRightFork())
                    {
                        return true; // Успех - обе вилки получены
                    }

                    attempts++;
                    if (attempts < maxAttempts)
                    {
                        Thread.Sleep(_random.Next(5, 25)); // Короткая пауза перед повторной попыткой
                    }
                }

                // Не удалось получить правую вилку - отпускаем левую
                philosopher.ReleaseLeftFork();
                // тк не удалось взять вилки - заставляем философа подождать перед повторной попыткой
                Thread.Sleep(_random.Next(10, 30));
                return false;
            }

            return philosopher.HasBothForks;
        }


    }
}
