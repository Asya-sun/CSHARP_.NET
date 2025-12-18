using Microsoft.AspNetCore.Mvc;
using Philosophers.Shared.DTO;
using System.Text.Json;
using TableService.Interfaces;
using TableService.Models.Enums;

namespace TableService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableController : ControllerBase
    {
        private readonly ITableManager _tableManager;
        private readonly IMetricsCollector _metricsCollector;
        private readonly ILogger<TableController> _logger;

        public TableController(
            ITableManager tableManager,
            IMetricsCollector metricsCollector,
            ILogger<TableController> logger)
        {
            _tableManager = tableManager;
            _metricsCollector = metricsCollector;
            _logger = logger;
        }

        // Регистрация философа
        [HttpPost("philosophers/register")]
        public IActionResult RegisterPhilosopher([FromBody] RegisterPhilosopherRequest request)
        {
            _logger.LogInformation("Регистрация философа: {Name}", request.Name);

            // Регистрируем в TableManagerService
            var registered = _tableManager.RegisterPhilosopher(
                request.PhilosopherId,
                request.Name,
                request.LeftForkId,
                request.RightForkId);

            if (!registered)
                return BadRequest("Не удалось зарегистрировать философа");

            return Ok(new
            {
                Message = $"Философ {request.Name} зарегистрирован",
                Timestamp = DateTime.UtcNow
            });
        }

        // Регистрация философа
        [HttpPost("philosophers/unregister")]
        public IActionResult UnregisterPhilosopher([FromBody] UnregisterPhilosopherRequest request)
        {
            _logger.LogInformation("Философ удалаяется: {id}", request.PhilosopherId);

            // Регистрируем в TableManagerService
            _tableManager.UnregisterPhilosopher(request.PhilosopherId);
            return Ok();
        }


        // Взять вилку
        [HttpPost("forks/{forkId}/acquire")]
        public async Task<ActionResult<AcquireForkResponse>> AcquireFork(
            int forkId,
            [FromBody] AcquireForkRequest request)
        {
            _logger.LogDebug("Запрос на взятие вилки {ForkId} от философа {PhilosopherId}",
                forkId, request.PhilosopherId);

            var success = await _tableManager.WaitForForkAsync(
                forkId,
                request.PhilosopherId,
                CancellationToken.None);

            return Ok(new AcquireForkResponse(success));
        }

        // Вернуть вилку
        [HttpPut("forks/{forkId}/release")]
        public IActionResult ReleaseFork(
            int forkId,
            [FromBody] ReleaseForkRequest request)
        {
            _logger.LogDebug("Возврат вилки {ForkId} от философа {PhilosopherId}",
                forkId, request.PhilosopherId);

            _tableManager.ReleaseFork(forkId, request.PhilosopherId);
            return Ok();
        }

        // Обновить состояние философа
        [HttpPut("philosophers/{philosopherId}/state")]
        public IActionResult UpdatePhilosopherState(
            string philosopherId,
            [FromBody] PhilosopherStateUpdate update)
        {

            _tableManager.UpdatePhilosopherState(
                philosopherId,
                MapToPhilosopherState(update.State),
                update.Action);

            return Ok();
        }

        // Получить состояние стола
        [HttpGet("state")]
        public IActionResult GetState()
        {
            var philosophers = _tableManager.GetAllPhilosophers()
                .Select(p => new PhilosopherDto(
                    Id: p.Id.ToString(),
                    Name: p.Name.ToString(),
                    State: p.State.ToString(),
                    Action: p.Action,
                    EatCount: p.EatCount
                )).ToList();

            var forks = _tableManager.GetAllForks()
                .Select(f => new ForkDto(
                    Id: f._id,
                    State: f._state.ToString(),
                    UsedBy: f._usedBy?.ToString()
                )).ToList();

            return Ok(new TableStateResponse(philosophers, forks));
        }

        private PhilosopherState MapToPhilosopherState(string state)
        {
            return state switch
            {
                "Thinking" => PhilosopherState.Thinking,
                "Hungry" => PhilosopherState.Hungry,
                "Eating" => PhilosopherState.Eating,
                _ => PhilosopherState.Thinking
            };
        }
    }
}
