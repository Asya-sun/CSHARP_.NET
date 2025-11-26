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
public class PoliteStrategyTests
{
    private (TestPhilosopher testPhilosopher, PoliteStrategy strategy, Mock<ITableManager> tableMock) CreatePhilosopher()
    {
        var tableMock = new Mock<ITableManager>();
        tableMock.Setup(t => t.GetPhilosopherForks(PhilosopherName.Socrates))
                 .Returns((0, 1));
        var metricsMock = new Mock<IMetricsCollector>();
        var options = Options.Create(new SimulationOptions { ForkAcquisitionTime = 0 });
        var philosopherLoggerMock = new Mock<ILogger<TestPhilosopher>>();
        var strategyLoggerMock = new Mock<ILogger<PoliteStrategy>>();
        var strategy = new PoliteStrategy(strategyLoggerMock.Object, options);

        var testPhilosopher =  new TestPhilosopher(
            PhilosopherName.Socrates,
            tableMock.Object,
            strategy,
            metricsMock.Object,
            options,
            philosopherLoggerMock.Object);

        return (testPhilosopher, strategy, tableMock);
    }

    // тестируем, что когда обе вилки доступны, то стратегия получает обе вилки
    [Fact]
    public async Task AcquireForks_Success_WhenBothForksAvailable()
    {
        // Arrange
        var (philosopher, strategy, tableMock) = CreatePhilosopher();

        tableMock.Setup(t => t.GetPhilosopherForks(philosopher.ExposedName))
                 .Returns((0, 1));

        tableMock.Setup(t => t.WaitForForkAsync(It.IsAny<int>(),
                                                philosopher.ExposedName,
                                                It.IsAny<CancellationToken>(),
                                                0))
                 .ReturnsAsync(true);

        // Act
        var result = await strategy.TryAcquireForksAsync(philosopher.ExposedName, tableMock.Object, CancellationToken.None);

        // Assert
        Assert.True(result);

        // проверка, что метод ReleaseFork не был вызван для этого философа
        tableMock.Verify(t => t.ReleaseFork(It.IsAny<int>(), philosopher.ExposedName), Times.Never);
    }


    // тестирует конкретно TryAcquireForksAsync
    [Fact]
    public async Task AcquireForks_Fails_WhenRightForkUnavailable()
    {
        // Arrange
        var (philosopher, strategy, tableMock) = CreatePhilosopher();

        tableMock.Setup(t => t.GetPhilosopherForks(philosopher.ExposedName))
                 .Returns((0, 1));

        // левая доступна
        tableMock.SetupSequence(t => t.WaitForForkAsync(0, philosopher.ExposedName, It.IsAny<CancellationToken>(), 0))
                 .ReturnsAsync(true);

        // правая недоступна
        tableMock.SetupSequence(t => t.WaitForForkAsync(1, philosopher.ExposedName, It.IsAny<CancellationToken>(), 0))
                 .ReturnsAsync(false);

        // Act
        var result = await strategy.TryAcquireForksAsync(philosopher.ExposedName, tableMock.Object, CancellationToken.None);

        // Assert
        Assert.False(result);

        // Левую вилку должны отпустить
        tableMock.Verify(t => t.ReleaseFork(0, philosopher.ExposedName), Times.Once);
        tableMock.Verify(t => t.ReleaseFork(1, philosopher.ExposedName), Times.Never);
    }



    // тестирует philosopher.RunOneIteration
    // state == Hungry → strategy.TryAcquireForksAsync(...)
    // возможно, этот тест лучше вынести в другой файл...
    [Fact]
    public async Task Hungry_FailsToTakeRightFork_ReleasesLeftFork()
    {
        // Arrange
        var (philosopher, strategy, tableMock) = CreatePhilosopher();

        // левая вилка доступна, правая нет
        tableMock.SetupSequence(t => t.WaitForForkAsync(It.IsAny<int>(), PhilosopherName.Socrates, It.IsAny<CancellationToken>(), 0))
                 .ReturnsAsync(true)  // левая
                 .ReturnsAsync(false); // правая

        // T → H
        await philosopher.RunOneIteration(CancellationToken.None);
        Assert.Equal(PhilosopherState.Hungry, philosopher.ExposedState);

        // H → H (неудача)
        await philosopher.RunOneIteration(CancellationToken.None);
        Assert.Equal(PhilosopherState.Hungry, philosopher.ExposedState);

        // левая должна быть отпущена
        tableMock.Verify(t => t.ReleaseFork(0, PhilosopherName.Socrates), Times.Once);
    }
}
