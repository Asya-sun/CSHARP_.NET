//using Philosophers.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface IPhilosopherStrategy
    {
        Task<bool> TryAcquireForksAsync(PhilosopherName philosopherName, ITableManager tableManager, CancellationToken cancellationToken);
        void ReleaseForks(PhilosopherName philosopherName, ITableManager tableManager);
    }
}