using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Services;
using Philosophers.Strategies;

namespace Philosophers.Tests;

public class DeadlockTests
{
    [Fact]
    public async Task StupidStrategy_GuaranteedDeadlock_WhenAllPhilosophersTryToEat()
    {
        // Arrange
        var tableManager = new TableManager(
            new Mock<ILogger<TableManager>>().Object,
            new Mock<IMetricsCollector>().Object);

        var optionsMock = new Mock<IOptions<SimulationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
        {
            ForkAcquisitionTime = 10
        });

        var stupidStrategy = new StupidStrategy(
            new Mock<ILogger<StupidStrategy>>().Object,
            optionsMock.Object);

        // Act - ВСЕ философы одновременно пытаются взять вилки
        var philosophers = new[] { "Платон", "Аристотель", "Сократ", "Декарт", "Кант" };
        var tasks = philosophers.Select(name =>
            stupidStrategy.TryAcquireForksAsync(name, tableManager, CancellationToken.None)
        ).ToArray();

        // Ждем достаточно долго чтобы дедлок проявился
        var completedTask = await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(3000));

        // Assert - ДЕДЛОК! Задачи не должны завершиться
        completedTask.Should().NotBeSameAs(tasks[0], "потому что должен быть дедлок");

        // Проверяем что все вилки заняты
        for (int i = 1; i <= 5; i++)
        {
            tableManager.GetForkState(i).Should().Be(ForkState.InUse, $"вилка {i} должна быть занята");
        }

        // Проверяем что никто не получил обе вилки
        var results = tasks.Select(t => t.IsCompleted ? t.Result : false).ToArray();
        results.Should().NotContain(true,"никто не должен получить обе вилки при дедлоке");
    }
}