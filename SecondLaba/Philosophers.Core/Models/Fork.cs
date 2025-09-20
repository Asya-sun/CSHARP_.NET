using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Core.Models
{
    public class Fork
    {
        private readonly object _lockObject = new object();
        private Philosopher? _currentUser { get; set; }

        public int _id { get; }
        public string _name => $"Fork-{_id}";
        public ForkState _state { get; private set; } = ForkState.Available;

        public Fork(int id)
        {
            _id = id;
        }


        public bool TryTake(Philosopher philosopher, int timeoutMs = 20)
        {
            if (Monitor.TryEnter(_lockObject, timeoutMs))
            {
                try
                {
                    if (_state == ForkState.Available)
                    {
                        _state = ForkState.InUse;
                        _currentUser = philosopher;
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
                    _state = ForkState.Available;
                    _currentUser = null;
                }
            }
        }


    }
}
