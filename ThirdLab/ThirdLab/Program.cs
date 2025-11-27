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

        // Создаем философа через рефлексию (более универсально)
        return Activator.CreateInstance(typeof(TPhilosopher),
            tableManager, strategy, metricsCollector, options, logger) as TPhilosopher;
    });
}




var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Конфигурация
        services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));

        // Основные сервисы
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        //services.AddSingleton<IPhilosopherStrategy, PoliteStrategy>();
        //services.AddSingleton<IPhilosopherStrategy, StupidStrategy>();
        services.AddTransient<StupidStrategy>();
        services.AddTransient<PoliteStrategy>();


        // Сервис отображения
        services.AddHostedService<DisplayService>();


        // Сервис детектор дедлоков
        services.AddHostedService<DeadlockDetector>();

        // Философы
        //services.AddHostedService<Plato>();
        //services.AddHostedService<Aristotle>();
        //services.AddHostedService<Socrates>();
        //services.AddHostedService<Decartes>();
        //services.AddHostedService<Kant>();

        RegisterPhilosopherWithStrategy<Plato, StupidStrategy>(services, PhilosopherName.Plato);
        RegisterPhilosopherWithStrategy<Aristotle, PoliteStrategy>(services, PhilosopherName.Aristotle);
        RegisterPhilosopherWithStrategy<Socrates, StupidStrategy>(services, PhilosopherName.Socrates);
        RegisterPhilosopherWithStrategy<Decartes, StupidStrategy>(services, PhilosopherName.Decartes);
        RegisterPhilosopherWithStrategy<Kant, StupidStrategy>(services, PhilosopherName.Kant);

        // Сервис для управления временем симуляции
        services.AddHostedService<SimulationHostedService>();

        //services.AddDbContext<SimulationDBContext>(options =>
        //    options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        //// Регистрируем репозиторий
        //services.AddScoped<ISimulationRepository, SimulationRepository>();

    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

await host.RunAsync();