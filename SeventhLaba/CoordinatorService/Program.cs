using CoordinatorService.Interfaces;
using CoordinatorService.Services;
using CoordinatorService.Models;
using CoordinatorService.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);


// CoordinatorState будет создаваться 1 раз 
// scoped живет в рамках 1 сообщения (вроде)
builder.Services.AddSingleton<CoordinatorState>();
builder.Services.AddScoped<ICoordinator, Coordinator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PhilosopherWantsToEatConsumer>();
    x.AddConsumer<PhilosopherFinishedEatingConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => "Coordinator is alive");

app.Run();
