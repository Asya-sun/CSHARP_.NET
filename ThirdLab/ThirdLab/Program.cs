using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Philosophers.Core.Models;
using Philosophers.Services;
using Philosophers.Core.Interfaces;
using Philosophers.Strategies;
using Philosophers.Services.Philosophers;
using System.Text;
using System.IO;
using Philosophers.DB.Context;
using Microsoft.EntityFrameworkCore;
using Philosophers.DB.Interfaces;

// For supporting Russian
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

//// Перенаправляем консольный вывод в файл
//var fileWriter = new StreamWriter("debug1.log") { AutoFlush = true };
//Console.SetOut(fileWriter);
//Console.SetError(fileWriter);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Конфигурация
        services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));

        // Основные сервисы
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        //services.AddSingleton<IPhilosopherStrategy, PoliteStrategy>();
        services.AddSingleton<IPhilosopherStrategy, StupidStrategy>();

        // Сервис отображения
        services.AddHostedService<DisplayService>();


        // Сервис детектор дедлоков
        services.AddHostedService<DeadlockDetector>();

        // Философы
        services.AddHostedService<Plato>();
        services.AddHostedService<Aristotle>();
        services.AddHostedService<Socrates>();
        services.AddHostedService<Decartes>();
        services.AddHostedService<Kant>();

        // Сервис для управления временем симуляции
        services.AddHostedService<SimulationHostedService>();

        services.AddDbContext<SimulationDBContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Регистрируем репозиторий
        services.AddScoped<ISimulationRepository, SimulationRepository>();

    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

await host.RunAsync();