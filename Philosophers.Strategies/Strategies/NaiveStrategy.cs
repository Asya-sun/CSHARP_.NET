using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using System;

namespace Philosophers.Strategies.Strategies
{
    public class NaiveStrategy : IPhilosopherStrategy
    {
        public string Name => "Naive";

        private Philosopher _philosopher = null!;
        private Random _random = new Random();

        // Для отслеживания процесса взятия вилок
        private int _leftForkTakeSteps = 0;
        private int _rightForkTakeSteps = 0;
        private bool _takingLeftFork = false;
        private bool _takingRightFork = false;

        // Для предотвращения deadlock
        private int _consecutiveFailures = 0;
        private int _stepsWithOneFork = 0;

        public void Initialize(Philosopher philosopher)
        {
            _philosopher = philosopher;
        }

        public void ExecuteStep()
        {
            if (_philosopher._state != PhilosopherState.Hungry)
                return;

            // Обрабатываем процесс взятия вилок
            ProcessForkTaking();

            // Если не в процессе взятия - пытаемся начать
            if (!_takingLeftFork && !_takingRightFork)
            {
                TryStartTakingForks();
            }

            // Проверяем, не пора ли освободить вилку чтобы избежать deadlock
            CheckForDeadlockPrevention();
        }

        private void ProcessForkTaking()
        {
            // Обработка взятия левой вилки
            if (_takingLeftFork)
            {
                _leftForkTakeSteps--;
                if (_leftForkTakeSteps <= 0)
                {
                    _takingLeftFork = false;
                    bool success = _philosopher.TryTakeLeftFork();
                    if (!success) _consecutiveFailures++;
                    CheckIfCanEat();
                }
                // Обработка взятия правой вилки
            } else if (_takingRightFork)
            {
                _rightForkTakeSteps--;
                if (_rightForkTakeSteps <= 0)
                {
                    _takingRightFork = false;
                    bool success = _philosopher.TryTakeRightFork();
                    if (!success) _consecutiveFailures++;
                    CheckIfCanEat();
                }
            }
        }

        private void TryStartTakingForks()
        {
            // Увеличиваем счетчик, если держим одну вилку
            if ((_philosopher.HasLeftFork && !_philosopher.HasRightFork) ||
                (!_philosopher.HasLeftFork && _philosopher.HasRightFork))
            {
                _stepsWithOneFork++;
            }
            else
            {
                _stepsWithOneFork = 0;
            }

            // Пытаемся взять левую вилку, если у нас ее нет
            if (!_philosopher.HasLeftFork && _philosopher.CanTakeLeftFork() && !_takingLeftFork)
            {
                _takingLeftFork = true;
                _leftForkTakeSteps = 2;
                return;
            }

            // Пытаемся взять правую вилку, если у нас ее нет
            if (!_philosopher.HasRightFork && _philosopher.CanTakeRightFork() && !_takingRightFork)
            {
                _takingRightFork = true;
                _rightForkTakeSteps = 2;
                return;
            }

            // Если обе вилки заняты, увеличиваем счетчик неудач
            if (!_philosopher.HasLeftFork && !_philosopher.HasRightFork)
            {
                _consecutiveFailures++;
            }
        }

        private void CheckForDeadlockPrevention()
        {
            // Освобождаем вилку если долго не можем взять вторую
            if (_stepsWithOneFork > 10)
            {
                if (_philosopher.HasLeftFork)
                {
                    _philosopher.ReleaseLeftFork();
                    _stepsWithOneFork = 0;
                    _consecutiveFailures = 0;
                }
                else if (_philosopher.HasRightFork)
                {
                    _philosopher.ReleaseRightFork();
                    _stepsWithOneFork = 0;
                    _consecutiveFailures = 0;
                }
            }

            // Освобождаем все вилки после множества неудач (предотвращение deadlock)
            if (_consecutiveFailures > 20)
            {
                _philosopher.ReleaseForks();
                _consecutiveFailures = 0;
                _stepsWithOneFork = 0;

                // Делаем небольшую паузу перед следующей попыткой
                _leftForkTakeSteps = _random.Next(1, 4);
                _rightForkTakeSteps = _random.Next(1, 4);
            }
        }

        private void CheckIfCanEat()
        {
            if (_philosopher.HasLeftFork && _philosopher.HasRightFork)
            {
                _philosopher.StartEating();
                _consecutiveFailures = 0;
                _stepsWithOneFork = 0;
            }
        }
    }
}