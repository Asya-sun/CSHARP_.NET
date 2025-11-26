using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.DB.Entities;
public class SimulationRun
{
    public int Id { get; set; }
    public Guid RunId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string OptionsJson { get; set; } = string.Empty;
}