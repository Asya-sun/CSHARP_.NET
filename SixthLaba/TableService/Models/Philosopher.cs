using TableService.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableService.Models
{
    public class Philosopher
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PhilosopherState State { get; set; }
        public int EatCount { get; set; }
        public string Action { get; set; } = "None";
        public bool _isFinished { get; set; } = false;

        public Philosopher(string id, string name, PhilosopherState state = PhilosopherState.Thinking)
        {
            Id = id;
            Name = name;
            State = state;
            EatCount = 0;
        }
    }
}
