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



        //public bool TryAcquireForks()
        //{
        //    // Сначала пробуем взять левую вилку
        //    if (_philosopher.LeftFork.TryTake(_philosopher))
        //    {
        //        // Если получилось, пробуем взять правую
        //        if (_philosopher.RightFork.TryTake(_philosopher))
        //        {
        //            return true; // Успешно взяли обе вилки
        //        }
        //        else
        //        {
        //            // Не получилось взять правую - отпускаем левую
        //            _philosopher.LeftFork.Release(_philosopher);
        //            return false;
        //        }
        //    }
        //    return false;
        //}



        public bool TryAcquireForks()
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} пытается взять вилки");

            if (_philosopher.LeftFork.TryTake(_philosopher))
            {
                //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} взял левую вилку");

                if (_philosopher.RightFork.TryTake(_philosopher))
                {
                    //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} взял правую вилку - УСПЕХ");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} не смог взять правую вилку");
                    _philosopher.LeftFork.Release(_philosopher);
                    return false;
                }
            }
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} не смог взять левую вилку");
            return false;
        }


    }
}
