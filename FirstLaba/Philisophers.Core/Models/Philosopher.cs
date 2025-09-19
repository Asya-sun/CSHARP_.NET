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

// мб замеминть TryTakeFork на Take...Fork с ассертом - все равно однопоточная программа
namespace Philosophers.Core.Models
{
    public class Philosopher
    {
        public int _id { get; set; }
        public string _name { get; set; } = string.Empty;
        public PhilosopherState _state { get; set; }
        public int _eatCount { get; set; }
        public int _stepsInCurrentState { get; set; }


        // Для отслеживания процесса взятия вилок
        public int _leftForkTakeSteps = 0;
        public int _rightForkTakeSteps = 0;
        public bool _takingLeftFork = false;
        public bool _takingRightFork = false;

        // Для предотвращения deadlock
        private int _consecutiveFailures = 0;
        private int _stepsWithOneFork = 0;




        // for statistics
        // Общее время в голоде
        public int _totalHungrySteps { get; set; }
        // Текущая серия голода
        public int _сurrentHungryStreak { get; set; }
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
                _totalHungrySteps++;
                _сurrentHungryStreak++;
                MaxHungryStreak = Math.Max(MaxHungryStreak, _сurrentHungryStreak);
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
                _сurrentHungryStreak = 0;
            } 
            else if (_takingLeftFork && _state == PhilosopherState.Hungry)
            {
                _leftForkTakeSteps--;
                // если сделали достаточно шагов, чтобы взять вилку
                if (_leftForkTakeSteps <= 0)
                {
                    _takingLeftFork = false;
                    TryStartEating();
                }
            } 
            else if (_takingRightFork && _state == PhilosopherState.Hungry)
            {
                _rightForkTakeSteps--;
                // если сделали достаточно шагов, чтобы взять вилку
                if (_rightForkTakeSteps <= 0)
                {                    
                    _takingRightFork = false;
                    TryStartEating();
                    
                }
            }


            // Если голоден и не берем никакие вилки
            // там внутри стратегия должна учесть, что философ уже мог взять вилки!!!
            // обращаемся к стратегии только если хотим есть и не берем сейчас вилку!!!
            if (_state == PhilosopherState.Hungry && !_takingRightFork && !_takingLeftFork)
            {
                Strategy.ExecuteStep(); // ← Стратегия пытается взять вилки
            }
        }

        // Методы для работы с вилками (стратегия использует их)
        public bool HasLeftFork => LeftFork.State == ForkState.InUse && LeftFork.CurrentUserId == _id && !_takingLeftFork;
        public bool HasRightFork => RightFork.State == ForkState.InUse && RightFork.CurrentUserId == _id && !_takingRightFork;

        public bool TakingLeftFork => LeftFork.State == ForkState.InUse && LeftFork.CurrentUserId == _id && _takingLeftFork;
        public bool TakingRightFork => RightFork.State == ForkState.InUse && RightFork.CurrentUserId == _id && _takingRightFork;

        public bool CanTakeLeftFork() => LeftFork.State == ForkState.Available;
        public bool CanTakeRightFork() => RightFork.State == ForkState.Available;

        public bool TryTakeLeftFork()
        {
            if (CanTakeLeftFork())
            {
                LeftFork.State = ForkState.InUse;
                LeftFork.CurrentUserId = _id;
                _takingLeftFork = true;
                _leftForkTakeSteps = 2;
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
                _takingRightFork = true;
                _rightForkTakeSteps = 2;
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

        public void TryStartEating()
        {
            if (HasLeftFork && HasRightFork && _state == PhilosopherState.Hungry)
            {
                _state = PhilosopherState.Eating;
                _stepsInCurrentState = 0;
            }
        }

        // если философ в процессе взятия вилки - он не обращается к стратегии
        // тк если он уже начал брать вилку, то она помечена, и никто ее не возьмет

        // ???
        public bool HasFork(Fork fork)
        {
            return HasLeftFork && LeftFork.Id == fork.Id ||
                   HasRightFork && RightFork.Id == fork.Id;
        }

        public bool IsStarving()
        {
            return _state == PhilosopherState.Hungry && _сurrentHungryStreak > 50;
        }

        public int GetHungerLevel()
        {
            return _сurrentHungryStreak;
        }

        public bool IsEatingNow()
        {
            return _state == PhilosopherState.Eating;
        }
    }
}
