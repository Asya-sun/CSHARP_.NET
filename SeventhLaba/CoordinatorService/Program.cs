using CoordinatorService.Interfaces;
using CoordinatorService.Services;
using CoordinatorService.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICoordinator, Coordinator>();

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
