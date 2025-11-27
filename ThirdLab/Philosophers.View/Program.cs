using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Context;
using Philosophers.DB.Interfaces;
using Philosophers.DB.Repositories;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

Guid runId;
TimeSpan simulationTime;

//dotnet run --runId 1af570b0-299b-4e8a-a1c3-f2ac602c388d --delay 4.12

// Обработка аргументов
if (args.Length == 0)
{
    Console.Write("введите RunId: ");
    var runIdInput = Console.ReadLine();

    if (!Guid.TryParse(runIdInput, out runId))
    {
        Console.WriteLine("неверный формат RunId");
        return 1;
    }

    Console.Write("введите время в секундах: ");
    var timeInput = Console.ReadLine();

    if (!double.TryParse(timeInput, out var delaySeconds))
    {
        Console.WriteLine("неверный формат времени ");
        return 1;
    }

    simulationTime = TimeSpan.FromSeconds(delaySeconds);
}
else if (args.Length == 4 && args[0] == "--runId" && args[2] == "--delay")
{
    if (!Guid.TryParse(args[1], out runId))
    {
        Console.WriteLine("неверный формат RunId");
        return 1;
    }

    if (!double.TryParse(args[3], out var delaySeconds))
    {
        Console.WriteLine("неверный формат времени");
        return 1;
    }

    simulationTime = TimeSpan.FromSeconds(delaySeconds);
}
else
{
    Console.WriteLine("Пупупу dotnet run --runId <GUID> --delay <seconds>");
    Console.WriteLine("Exmaple dotnet run --runId 1af570b0-299b-4e8a-a1c3-f2ac602c388d --delay 4.12");
    return 1;
}

// Настройка конфигурации
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
    loggingBuilder.AddConsole();
});

services.AddDbContextFactory<SimulationDBContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

services.AddScoped<ISimulationRepository, SimulationRepository>();

var serviceProvider = services.BuildServiceProvider();

try
{
    await DisplaySimulationState(runId, simulationTime, serviceProvider);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return 1;
}

static async Task DisplaySimulationState(Guid runId, TimeSpan simulationTime, IServiceProvider serviceProvider)
{
    var repository = serviceProvider.GetRequiredService<ISimulationRepository>();

    var run = await repository.GetRunAsync(runId);
    if (run == null)
    {
        Console.WriteLine($"Запуск с RunId {runId} не найден");
        return;
    }

    Console.WriteLine($"=== Состояние симуляции RunId: {runId} ===");
    Console.WriteLine($"Время симуляции: {simulationTime:hh\\:mm\\:ss\\.ff}");
    Console.WriteLine($"Запущена: {run.StartedAt:yyyy-MM-dd HH:mm:ss}");
    if (run.FinishedAt.HasValue)
    {
        Console.WriteLine($"Завершена: {run.FinishedAt:yyyy-MM-dd HH:mm:ss}");
    }
    Console.WriteLine();

    // Получаем состояния философов
    var philosopherStates = await repository.GetPhilosopherStatesAtTimeAsync(runId, simulationTime);
    Console.WriteLine("Философы:");
    foreach (var state in philosopherStates.OrderBy(p => p.PhilosopherName))
    {
        var philosopherName = state.PhilosopherName.ToString();
        var stateName = state.State.ToString();
        var action = string.IsNullOrEmpty(state.Action) ? "" : $" ({state.Action})";
        Console.WriteLine($"  {philosopherName}: {stateName}{action}, Стратегия: {state.StrategyName}");
    }

    Console.WriteLine();

    // Получаем состояния вилок
    var forkStates = await repository.GetForkStatesAtTimeAsync(runId, simulationTime);
    Console.WriteLine("Вилки:");
    foreach (var fork in forkStates.OrderBy(f => f.ForkId))
    {
        var usedBy = fork.UsedBy.HasValue ? $" (используется {fork.UsedBy})" : "";
        Console.WriteLine($"  Вилка-{fork.ForkId}: {fork.State}{usedBy}");
    }

    Console.WriteLine();

    // Проверяем дедлоки
    var deadlocks = await repository.GetDeadlocksAsync(runId);
    var deadlocksAtTime = deadlocks.Where(d => d.SimulationTime <= simulationTime).ToList();

    if (deadlocksAtTime.Any())
    {
        Console.WriteLine("Дедлоки:");
        foreach (var deadlock in deadlocksAtTime)
        {
            Console.WriteLine($"  Дедлок #{deadlock.DeadlockNumber} в {deadlock.SimulationTime:hh\\:mm\\:ss\\.ff} - разрешен {deadlock.ResolvedByPhilosopher}");
        }
    }
    else
    {
        Console.WriteLine("Дедлоков не обнаружено");
    }
}