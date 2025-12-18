using TableService.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TableService.Models
{
    public class Fork
    {
        public int _id { get; set; }
        public ForkState _state { get; set; }
        public string? _usedBy { get; set; }

        public Fork(int id)
        {
            _id = id;
            _state = ForkState.Available;
        }

        public Fork() { }
    }
}
