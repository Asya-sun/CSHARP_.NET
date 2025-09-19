using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;


namespace Philosophers.Strategies.Services
{
    public class Coordinator : ICoordinator
    {
        private readonly List<Philosopher> _philosophers;
        private readonly PriorityQueue<int, int> _requestQueue; // PhilosopherId, HungerLevel
        private readonly Dictionary<int, int> _hungerLevels = new();

        public event Action<int, ForkAction>? OnForkActionAllowed;

        public Coordinator(List<Philosopher> philosophers, List<Fork> forks)
        {
            _philosophers = philosophers;
            _requestQueue = new PriorityQueue<int, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        }

        public void RequestToEat(int philosopherId)
        {
            // ищем подходящего философа
            var philosopher = _philosophers.First(p => p._id == philosopherId);
            // смотрим, насколько он голоден
            int hungerLevel = philosopher.GetHungerLevel();

            // добавляем философа с нужным  id в список сс уровнем голода
            _hungerLevels[philosopherId] = hungerLevel;

            // Добавляем в очередь с приоритетом (бОольший голод = высший приоритет)
            // если философа нет в очереди, то добавляем его туда
            if (!_requestQueue.UnorderedItems.Any(x => x.Element == philosopherId))
            {
                _requestQueue.Enqueue(philosopherId, hungerLevel);
            }

            ProcessRequests();
        }

        private void ProcessRequests()
        {
            var tempQueue = new PriorityQueue<int, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));

            while (_requestQueue.Count > 0)
            {
                // извлекаем id философа с самым большим приоритетом
                var philosopherId = _requestQueue.Dequeue();
                // затем самого философа с этим id
                var philosopher = _philosophers.First(p => p._id == philosopherId);

                if (TryGrantForks(philosopher))
                {
                    // Успешно выдали вилки - удаляем из очереди
                    _hungerLevels.Remove(philosopherId);
                }
                else
                {
                    // Не смогли выдать - возвращаем в очередь
                    tempQueue.Enqueue(philosopherId, _hungerLevels[philosopherId]);
                }
            }

            // Восстанавливаем очередь
            while (tempQueue.Count > 0)
            {
                var item = tempQueue.Dequeue();
                _requestQueue.Enqueue(item, _hungerLevels[item]);
            }
        }

        private bool TryGrantForks(Philosopher philosopher)
        {
            // Проверяем, может ли философ взять вилки без создания deadlock
            bool canTakeLeft = philosopher.CanTakeLeftFork() &&
                              !WouldBlockStarvingPhilosopher(philosopher, philosopher.LeftFork);

            bool canTakeRight = philosopher.CanTakeRightFork() &&
                               !WouldBlockStarvingPhilosopher(philosopher, philosopher.RightFork);

            // Выдаем вилки по одной, начиная с той, что меньше блокирует других
            if (canTakeLeft && canTakeRight)
            {
                // Берем ту, что меньше нужна другим голодающим
                if (GetForkStarvationLevel(philosopher.LeftFork) > GetForkStarvationLevel(philosopher.RightFork))
                {
                    OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeRight);
                    //OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeLeft);
                }
                else
                {
                    OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeLeft);
                    //OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeRight);
                }
                return true;
            }
            else if (canTakeLeft && !philosopher.HasLeftFork && !philosopher.TakingLeftFork)
            {
                OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeLeft);
                return true;
            }
            else if (canTakeRight && !philosopher.HasRightFork && !philosopher.TakingRightFork)
            {
                OnForkActionAllowed?.Invoke(philosopher._id, ForkAction.TakeRight);
                return true;
            }

            return false;
        }

        private bool WouldBlockStarvingPhilosopher(Philosopher requester, Fork fork)
        {
            // Находим философа, который использует эту вилку
            var currentUser = _philosophers.FirstOrDefault(p => p.HasFork(fork));

            if (currentUser == null) return false;

            // Если текущий пользователь голодает дольше, чем запрашивающий - блокируем запрос
            // Небольшой плюс-минус на всякий случай
            // если текущий владелец вилки ест, то отобрать у него вилку нельзя никак
            return currentUser.GetHungerLevel() > requester.GetHungerLevel() + 5 || currentUser.IsEatingNow(); 
        }

        private int GetForkStarvationLevel(Fork fork)
        {
            // Возвращает уровень "голода" для вилки (сколько философов ждут ее)
            return _philosophers.Count(p =>
                (p.LeftFork.Id == fork.Id && !p.HasLeftFork && p._state == PhilosopherState.Hungry) ||
                (p.RightFork.Id == fork.Id && !p.HasRightFork && p._state == PhilosopherState.Hungry));
        }

        public void ReleaseForks(int philosopherId)
        {
            OnForkActionAllowed?.Invoke(philosopherId, ForkAction.ReleaseLeft);
            OnForkActionAllowed?.Invoke(philosopherId, ForkAction.ReleaseRight);
            ProcessRequests(); // Перераспределяем вилки после освобождения
        }
    }
}