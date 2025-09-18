using Philosophers.Core.Models.Enums;
using Philosophers.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*
 * Философ (бизнес-логика):
 * Управление своим состоянием
 * Предоставление методов для работы с вилками
 * Контроль времени в состояниях
*/
namespace Philosophers.Core.Models
{
    public class Philosopher
    {
        public int _id { get; set; }
        public string _name { get; set; } = string.Empty;
        public PhilosopherState _state { get; set; }
        public int _eatCount { get; set; }
        public int _stepsInCurrentState { get; set; }




        // for statistics
        // Общее время в голоде
        public int TotalHungrySteps { get; set; }
        // Текущая серия голода
        public int CurrentHungryStreak { get; set; }
        // Максимальное время голода
        public int MaxHungryStreak { get; set; }
        // Шаг последней еды
        public int LastEatStep { get; set; } 

        // ???
        public IPhilosopherStrategy Strategy { get; set; } = null!;
        public Fork LeftFork { get; set; } = null!;
         public Fork RightFork { get; set; } = null!;
        private readonly Random _random = new();
        private int _currentThinkingTime;
        private int _currentEatingTime;

        public Philosopher()
        {
            _currentThinkingTime = _random.Next(3, 11);
            _currentEatingTime = _random.Next(4, 6);
        }

        public void ExecuteStep(int currentStep)
        {
            _stepsInCurrentState++;

            // for statistics
            if (_state == PhilosopherState.Hungry)
            {
                TotalHungrySteps++;
                CurrentHungryStreak++;
                MaxHungryStreak = Math.Max(MaxHungryStreak, CurrentHungryStreak);
            }
            else
            {
                CurrentHungryStreak = 0;
            }

            // Автоматические переходы
            if (_state == PhilosopherState.Thinking && _stepsInCurrentState >= _currentThinkingTime)
            {
                _state = PhilosopherState.Hungry;
                _stepsInCurrentState = 0;
                // for next thinking
                _currentThinkingTime = _random.Next(3, 11);
            }
            else if (_state == PhilosopherState.Eating && _stepsInCurrentState >= _currentEatingTime)
            {
                _state = PhilosopherState.Thinking;
                _stepsInCurrentState = 0;
                _eatCount++;
                LastEatStep = currentStep;
                ReleaseForks();
                // for next eating
                _currentEatingTime = _random.Next(4, 6);
            }

            // Если голоден - вызываем стратегию
            if (_state == PhilosopherState.Hungry)
            {
                Strategy.ExecuteStep(); // ← Стратегия пытается взять вилки
            }
        }

        // Методы для работы с вилками (стратегия использует их)
        public bool HasLeftFork => LeftFork.State == ForkState.InUse && LeftFork.CurrentUserId == _id;
        public bool HasRightFork => RightFork.State == ForkState.InUse && RightFork.CurrentUserId == _id;

        public bool CanTakeLeftFork() => LeftFork.State == ForkState.Available;
        public bool CanTakeRightFork() => RightFork.State == ForkState.Available;

        public bool TryTakeLeftFork()
        {
            if (CanTakeLeftFork())
            {
                LeftFork.State = ForkState.InUse;
                LeftFork.CurrentUserId = _id;
                return true;
            }
            return false;
        }

        public bool TryTakeRightFork()
        {
            if (CanTakeRightFork())
            {
                RightFork.State = ForkState.InUse;
                RightFork.CurrentUserId = _id;
                return true;
            }
            return false;
        }
        public void ReleaseForks()
        {
            ReleaseLeftFork();
            ReleaseRightFork();
        }

        public void ReleaseLeftFork()
        {
            if (HasLeftFork)
            {
                LeftFork.State = ForkState.Available;
                LeftFork.CurrentUserId = null;
            }
        }

        public void ReleaseRightFork()
        {
            if (HasRightFork)
            {
                RightFork.State = ForkState.Available;
                RightFork.CurrentUserId = null;
            }
        }

        public void StartEating()
        {
            if (HasLeftFork && HasRightFork)
            {
                _state = PhilosopherState.Eating;
                _stepsInCurrentState = 0;
            }
        }

        public bool HasFork(Fork fork)
        {
            return HasLeftFork && LeftFork.Id == fork.Id ||
                   HasRightFork && RightFork.Id == fork.Id;
        }

        public bool IsStarving()
        {
            return _state == PhilosopherState.Hungry && TotalHungrySteps > 50;
        }

        public int GetHungerLevel()
        {
            return TotalHungrySteps;
        }
    }
}
