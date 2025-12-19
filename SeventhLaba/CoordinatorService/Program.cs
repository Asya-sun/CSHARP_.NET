using CoordinatorService.Consumers;
using CoordinatorService.Interfaces;
using CoordinatorService.Models;
using CoordinatorService.Services;
using MassTransit;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var config = new CoordinatorConfig
{
    PhilosophersCount = int.Parse(Environment.GetEnvironmentVariable("PHILOSOPHER_COUNT") ?? "5"),
};

// Регистрируем конфигурацию
builder.Services.AddSingleton(Options.Create(config));

// CoordinatorState будет создаваться 1 раз 
// scoped живет в рамках 1 сообщения (вроде)
builder.Services.AddSingleton<CoordinatorState>();
builder.Services.AddScoped<ICoordinator, Coordinator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PhilosopherWantsToEatConsumer>();
    x.AddConsumer<PhilosopherFinishedEatingConsumer>();
    x.AddConsumer<PhilosopherExitingConsumer>();
    x.AddConsumer<PhilosopherRegisteredConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");

        });
        cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(5)));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => "Coordinator is alive");

app.Run();
