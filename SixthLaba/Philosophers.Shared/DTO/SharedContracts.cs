using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Shared.DTO
{
    // Для регистрации философа на столе
    public record RegisterPhilosopherRequest(
        string PhilosopherId,
        string Name,
        int LeftForkId,
        int RightForkId         
    );

    public record UnregisterPhilosopherRequest(
        string PhilosopherId
    );

    // Для запроса на взятие вилки
    public record AcquireForkRequest(
        string PhilosopherId,
        int ForkId
    );

    public record AcquireForkResponse(bool Success);

    // Для возврата вилки
    public record ReleaseForkRequest(
        string PhilosopherId,
        int ForkId
    );

    // Состояние философа для TableService
    public record PhilosopherStateUpdate(
        string PhilosopherId,
        string State,          // "Thinking", "Hungry", "Eating"
        string Action,         // "TakeLeftFork", "Eating", etc.
        int? StepsLeft,
        int EatCount
    );

    
    // DTO для ответов API
    public record ForkDto(
        int Id,
        string State,           // "Available", "InUse"
        string? UsedBy          // null или "Платон"
    );

    public record PhilosopherDto(
        string Id,
        string Name,
        string State,
        string Action,
        int EatCount
    );

    public record TableStateResponse(
        List<PhilosopherDto> Philosophers,
        List<ForkDto> Forks
    );

    public record MetricsResponse(
        Dictionary<string, int> EatCounts,
        Dictionary<string, double> AverageWaitingTimes,
        Dictionary<int, double> ForkUtilization
    );
}
