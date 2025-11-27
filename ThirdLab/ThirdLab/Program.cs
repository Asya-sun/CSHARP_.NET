using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.DB.Context;
using Philosophers.DB.Interfaces;
using Philosophers.DB.Repositories;
using Philosophers.Services;
using Philosophers.Services.Philosophers;
using Philosophers.Strategies;
using System.IO;
using System.Text;

// For supporting Russian
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

//// Перенаправляем консольный вывод в файл
//var fileWriter = new StreamWriter("debug1.log") { AutoFlush = true };
//Console.SetOut(fileWriter);
//Console.SetError(fileWriter);
static void RegisterPhilosopherWithStrategy<TPhilosopher, TStrategy>(
    IServiceCollection services,
    PhilosopherName name)
    where TPhilosopher : PhilosopherHostedService
    where TStrategy : class, IPhilosopherStrategy
{
    services.AddTransient<TStrategy>();

    services.AddHostedService(provider =>
    {
        var strategy = provider.GetRequiredService<TStrategy>();
        var tableManager = provider.GetRequiredService<ITableManager>();
        var metricsCollector = provider.GetRequiredService<IMetricsCollector>();
        var options = provider.GetRequiredService<IOptions<SimulationOptions>>();
        var logger = provider.GetRequiredService<ILogger<TPhilosopher>>();
        var repository = provider.GetRequiredService<ISimulationRepository>();
        var runIdService = provider.GetRequiredService<RunIdService>();

        // Создаем философа через рефлексию (более универсально)
        return Activator.CreateInstance(typeof(TPhilosopher),
            tableManager, strategy, metricsCollector, options, logger, repository, runIdService) as TPhilosopher;
    });
}




var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Конфигурация
        services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));
        services.AddScoped<RunIdService>();

        // бд - используем DbContextFactory для многопоточности
        services.AddDbContextFactory<SimulationDBContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Регистрируем репозиторий как Transient
        services.AddTransient<ISimulationRepository, SimulationRepository>();
        services.AddScoped<RunIdService>();

        // Основные сервисы
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        services.AddTransient<StupidStrategy>();
        services.AddTransient<PoliteStrategy>();




        // Сервис отображения
        services.AddHostedService<DisplayService>();
        // Сервис детектор дедлоков
        services.AddHostedService<DeadlockDetector>();
        // Сервис для управления временем симуляции
        services.AddHostedService<SimulationHostedService>();

        // Философы
        RegisterPhilosopherWithStrategy<Plato, StupidStrategy>(services, PhilosopherName.Plato);
        RegisterPhilosopherWithStrategy<Aristotle, PoliteStrategy>(services, PhilosopherName.Aristotle);
        RegisterPhilosopherWithStrategy<Socrates, StupidStrategy>(services, PhilosopherName.Socrates);
        RegisterPhilosopherWithStrategy<Decartes, StupidStrategy>(services, PhilosopherName.Decartes);
        RegisterPhilosopherWithStrategy<Kant, StupidStrategy>(services, PhilosopherName.Kant);
        
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();


using (var scope = host.Services.CreateScope())
{
    try
    {
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SimulationDBContext>>();
        using var context = await contextFactory.CreateDbContextAsync();

        // миграции
        await context.Database.MigrateAsync();
        Console.WriteLine("миграции применены");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ошибка миграций: {ex.Message}");
        throw;
    }
}


await host.RunAsync();