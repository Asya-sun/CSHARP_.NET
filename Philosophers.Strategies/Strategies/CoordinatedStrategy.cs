using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Strategies.Strategies
{
    public class CoordinatedStrategy : ICoordinatedStrategy
    {
        private Philosopher _philosopher = null!;
        private ICoordinator _coordinator = null!;
        private int _consecutiveFailures = 0;

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

            // Если много неудачных попыток - возможно, стоит помочь другим
            if (_consecutiveFailures > 10 && ShouldReleaseToHelpOthers())
            {
                ReleaseToPreventStarvation();
                _consecutiveFailures = 0;
                return;
            }

            // Стандартная логика запроса
            if (!_philosopher.HasLeftFork || !_philosopher.HasRightFork)
            {
                _coordinator.RequestToEat(_philosopher._id);
            }
        }

        private bool ShouldReleaseToHelpOthers()
        {
            // Проверяем, есть ли философы, которые голодают дольше нас
            // и которым могут помочь наши вилки
            return (_philosopher.HasLeftFork && WouldHelpOthers(_philosopher.LeftFork)) ||
                   (_philosopher.HasRightFork && WouldHelpOthers(_philosopher.RightFork));
        }

        private bool WouldHelpOthers(Fork fork)
        {
            // Здесь должна быть логика проверки, поможет ли освобождение вилки другим
            // В реальной реализации нужно получить эту информацию от координатора
            return fork.State == ForkState.InUse &&
                   _philosopher.GetHungerLevel() < 50; // Если мы не слишком голодны сами
        }

        private void ReleaseToPreventStarvation()
        {
            // Освобождаем одну вилку чтобы помочь другим
            if (_philosopher.HasLeftFork && WouldHelpOthers(_philosopher.LeftFork))
            {
                _philosopher.ReleaseLeftFork();
            }
            else if (_philosopher.HasRightFork && WouldHelpOthers(_philosopher.RightFork))
            {
                _philosopher.ReleaseRightFork();
            }
        }

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
                _consecutiveFailures = 0;
                if (_philosopher.HasLeftFork && _philosopher.HasRightFork)
                {
                    _philosopher.StartEating();
                }
            }
            else
            {
                _consecutiveFailures++;
            }
        }
    }
}