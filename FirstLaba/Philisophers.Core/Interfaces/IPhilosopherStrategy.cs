using Philosophers.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



/*
 * Стратегия (алгоритмическая логика):
 * Принятие решений КОГДА пытаться взять/отпустить вилки
 * Определение ПОРЯДКА взятия вилок
 * Логика предотвращения deadlock
 */
namespace Philosophers.Core.Interfaces
{
    public interface IPhilosopherStrategy
    {
        string Name { get; }
        void Initialize(Philosopher philosopher);
        void ExecuteStep();
    }

    public interface ICoordinatedStrategy : IPhilosopherStrategy
    {
        void SetCoordinator(ICoordinator coordinator);
    }
}
