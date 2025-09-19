using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Interfaces
{
    public interface ICoordinator
    {
        event Action<int, ForkAction>? OnForkActionAllowed;
        void RequestToEat(int philosopherId);
        void ReleaseForks(int philosopherId);
    }

    public enum ForkAction { TakeLeft, TakeRight, ReleaseLeft, ReleaseRight, None }
}
