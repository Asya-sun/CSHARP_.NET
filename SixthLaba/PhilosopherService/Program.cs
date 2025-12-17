using Microsoft.Extensions.Options;
using PhilosopherService.Http;
using PhilosopherService.Models;
using PhilosopherService.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация из переменных окружения
var config = new PhilosopherConfig
{
    PhilosopherId = Environment.GetEnvironmentVariable("PHILOSOPHER_ID") ?? "MyUnknownPhilosopher",
    Name = Environment.GetEnvironmentVariable("PHILOSOPHER_NAME") ?? "MyUnknownPhilosopherName",
    LeftForkId = int.Parse(Environment.GetEnvironmentVariable("LEFT_FORK_ID") ?? "1"),
    RightForkId = int.Parse(Environment.GetEnvironmentVariable("RIGHT_FORK_ID") ?? "2"),
    TableServiceUrl = Environment.GetEnvironmentVariable("TABLE_SERVICE_URL") ?? "http://localhost:5178",
    SimulationDurationMinutes = int.Parse(Environment.GetEnvironmentVariable("SIMULATION_DURATION_MINUTES") ?? "1")
};

// Регистрируем конфигурацию
builder.Services.AddSingleton(Options.Create(config));

// Настраиваем HttpClient для TableClient
builder.Services.AddHttpClient<TableClient>(client =>
{
    client.BaseAddress = new Uri(config.TableServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy());

// Регистрируем сервисы
builder.Services.AddHostedService<PhilosopherHostedService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
Console.WriteLine($"TABLE_SERVICE_URL={config.TableServiceUrl}");
Console.WriteLine($"PHILOSOPHER_NAME={config.Name}");
Console.WriteLine($"PHILOSOPHER_ID={config.PhilosopherId}");

app.MapControllers();
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Philosopher = config.Name,
    Id = config.PhilosopherId,
    Forks = $"{config.LeftForkId}/{config.RightForkId}",
    Eaten = "N/A" // Можно добавить счетчик
});

app.MapGet("/", () => $"Philosopher {config.Name} is running!");

app.Run();

// Политика повторных попыток для HTTP-запросов
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}