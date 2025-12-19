using Microsoft.Extensions.Options;
using TableService.Interfaces;
using TableService.Services;
using TableService.Models;

var builder = WebApplication.CreateBuilder(args);

var config = new TableConfig
{
    PhilosophersCount = int.Parse(Environment.GetEnvironmentVariable("PHILOSOPHER_COUNT") ?? "5"),
    TableServiceUrl = Environment.GetEnvironmentVariable("TABLE_SERVICE_URL") ?? "http://localhost:5178",
};

// Регистрируем конфигурацию
builder.Services.AddSingleton(Options.Create(config));


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ваши сервисы
builder.Services.AddSingleton<ITableManager, TableManagerService>();
//builder.Services.AddSingleton<IMetricsCollector, MetricsCollectorService>();
builder.Services.AddSingleton<ITableMetricsCollector, TableMetricsCollectorService>();
builder.Services.AddHostedService<DeadlockDetector>();

// Настройка CORS (для локальной разработки)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Table Service is running!");
app.MapGet("/health", () => new { Status = "Healthy", Time = DateTime.UtcNow });

app.Run();
