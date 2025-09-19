using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Strategies.Strategies
{
    public class CoordinatedStrategy : ICoordinatedStrategy
    {
        private Philosopher _philosopher = null!;
        private ICoordinator _coordinator = null!;

        public string Name => "Cooperative";

        public void Initialize(Philosopher philosopher) => _philosopher = philosopher;

        public void SetCoordinator(ICoordinator coordinator)
        {
            _coordinator = coordinator;
            _coordinator.OnForkActionAllowed += OnForkActionAllowed;
        }

        public void ExecuteStep()
        {
            if (_philosopher._state != PhilosopherState.Hungry) return;

         
            // Стандартная логика запроса
            if (!_philosopher.HasLeftFork || !_philosopher.HasRightFork)
            {
                _coordinator.RequestToEat(_philosopher._id);
            }
        }

        // вот я хз - это тут должно быть или нет? Вроде да, а вроде и хз
        private void OnForkActionAllowed(int philosopherId, ForkAction action)
        {
            if (philosopherId != _philosopher._id) return;

            bool success = false;
            switch (action)
            {
                case ForkAction.TakeLeft:
                    success = _philosopher.TryTakeLeftFork();
                    break;
                case ForkAction.TakeRight:
                    success = _philosopher.TryTakeRightFork();
                    break;
            }

            if (success)
            {
                if (_philosopher.HasLeftFork && _philosopher.HasRightFork)
                {
                    _philosopher.TryStartEating();
                }
            }
        }
    }
}