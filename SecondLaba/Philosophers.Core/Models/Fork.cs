using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// ? вынести метрики вилки в отдельный класс?


namespace Philosophers.Core.Models
{
    public class Fork
    {
        private readonly object _lockObject = new object();
        private readonly Stopwatch _usageTimer = new Stopwatch();
        public Philosopher? _currentUser { get; private set; }


        // Метрики
        public long TotalInUseTimeMs { get; private set; }
        public long TotalAvailableTimeMs { get; private set; }
        public DateTime _lastStateChange { get; private set; } = DateTime.Now;

        public int _id { get; }
        public string _name => $"Fork-{_id}";
        public ForkState _state { get; private set; } = ForkState.Available;

        public Fork(int id)
        {
            _id = id;
            _lastStateChange = DateTime.Now;
        }


        public bool TryTake(Philosopher philosopher, int timeoutMs = 10)
        {

            if (Monitor.TryEnter(_lockObject, timeoutMs))
            {
                try
                {
                    // update metrics before changing state
                    UpdateMetrics();

                    if (_state == ForkState.Available)
                    {
                        _state = ForkState.InUse;
                        _currentUser = philosopher;
                        _lastStateChange = DateTime.Now;
                        _usageTimer.Restart();

                        Thread.Sleep(20);

                        return true;
                    }
                }
                finally
                {
                    Monitor.Exit(_lockObject);
                }
            }
            return false;
        }

        public void Release(Philosopher philosopher)
        {
            lock (_lockObject)
            {
                if (_currentUser == philosopher)
                {
                    // update metrics before changing state
                    UpdateMetrics();
                    _state = ForkState.Available;
                    _currentUser = null;

                    _lastStateChange = DateTime.Now;
                    _usageTimer.Stop();
                    //TotalInUseTimeMs += _usageTimer.ElapsedMilliseconds;
                }
            }
        }


        public void UpdateMetrics()
        {
            var now = DateTime.Now;
            var timeSinceLastChange = (now - _lastStateChange).TotalMilliseconds;

            if (_state == ForkState.Available)
            {
                TotalAvailableTimeMs += (long)timeSinceLastChange;
            }
            else
            {
                TotalInUseTimeMs += (long)timeSinceLastChange;
            }

            _lastStateChange = now;
        }


        public double GetUtilizationPercentage(long totalSimulationTimeMs)
        {
            if (totalSimulationTimeMs == 0) return 0;
            //return (double)TotalInUseTimeMs / totalSimulationTimeMs * 100;

            // Убедимся, что время использования не превышает общее время симуляции
            var actualInUseTime = Math.Min(TotalInUseTimeMs, totalSimulationTimeMs);
            return (double)actualInUseTime / totalSimulationTimeMs * 100;
        }
    }
}
