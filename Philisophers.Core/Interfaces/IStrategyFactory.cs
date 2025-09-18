using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface IStrategyFactory
    {
        IPhilosopherStrategy CreateStrategy(string strategyName, ICoordinator coordinator = null);
    }
}
