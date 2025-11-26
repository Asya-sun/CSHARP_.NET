using Philosophers.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;

public class PhilosopherStateChange
{
    public int Id { get; set; }
    public Guid RunId { get; set; }
    public PhilosopherName PhilosopherName { get; set; }
    public PhilosopherState State { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TimeSpan SimulationTime { get; set; }
}
