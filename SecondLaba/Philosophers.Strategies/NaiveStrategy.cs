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
        
        

        
        public bool requestOnEat(Philosopher philosopher)
        {

            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} пытается взять вилки");

            if (philosopher.LeftFork.TryTake(philosopher))
            {
                //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} взял левую вилку");

                if (philosopher.RightFork.TryTake(philosopher))
                {
                    //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} взял правую вилку - УСПЕХ");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} не смог взять правую вилку");
                    philosopher.LeftFork.Release(philosopher);
                    return false;
                }
            }
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {_philosopher._name} не смог взять левую вилку");
            return false;
        }


    }
}
