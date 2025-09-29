using Philosophers.Core.Metrics;
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
        public ForkMetrics Metrics { get; private set; }
        
        public int _id { get; }
        public string _name => $"Fork-{_id}";
        public ForkState _state { get; private set; } = ForkState.Available;

        public Fork(int id)
        {
            _id = id;
            Metrics = new ForkMetrics(_name);
            Metrics._lastStateChange = DateTime.Now;
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
                        Metrics._lastStateChange = DateTime.Now;
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

                    Metrics._lastStateChange = DateTime.Now;
                    _usageTimer.Stop();
                    //TotalInUseTimeMs += _usageTimer.ElapsedMilliseconds;
                }
            }
        }


        public void UpdateMetrics()
        {
            var now = DateTime.Now;
            var timeSinceLastChange = (now - Metrics._lastStateChange).TotalMilliseconds;

            if (_state == ForkState.Available)
            {
                Metrics.TotalAvailableTimeMs += (long)timeSinceLastChange;
            }
            else
            {
                Metrics.TotalInUseTimeMs += (long)timeSinceLastChange;
            }

            Metrics._lastStateChange = now;
        }


        public double GetUtilizationPercentage(long totalSimulationTimeMs)
        {
            return Metrics.GetUtilizationPercentage(totalSimulationTimeMs);
        }
    }
}
