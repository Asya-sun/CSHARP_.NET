using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;
public class SimulationRun
{
    public int RunId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string OptionsJson { get; set; } = string.Empty;


    public ICollection<PhilosopherStateChange> PhilosopherStateChanges { get; set; } = new List<PhilosopherStateChange>();
    public ICollection<ForkStateChange> ForkStateChanges { get; set; } = new List<ForkStateChange>();
    public ICollection<DeadlockRecord> DeadlockRecords { get; set; } = new List<DeadlockRecord>();
}