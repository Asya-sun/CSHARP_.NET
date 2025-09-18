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
        public int Id { get; set; }
        public ForkState State { get; set; }
        public int? CurrentUserId { get; set; }

    }
}
