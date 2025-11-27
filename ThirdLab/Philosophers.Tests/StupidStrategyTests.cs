using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Interfaces;
using Philosophers.Services;
using Philosophers.Strategies;

namespace Philosophers.Tests;
public class StupidStrategyPhilosopherTests
{
    private (TestPhilosopher testPhilosopher, StupidStrategy strategy, Mock<ITableManager> tableMock) CreatePhilosopher()
    {
        var tableMock = new Mock<ITableManager>();
        tableMock.Setup(t => t.GetPhilosopherForks(PhilosopherName.Socrates))
                 .Returns((0, 1));
        var metricsMock = new Mock<IMetricsCollector>();
        var options = Options.Create(new SimulationOptions { ForkAcquisitionTime = 0 });
        var philosopherLoggerMock = new Mock<ILogger<TestPhilosopher>>();
        var strategyLoggerMock = new Mock<ILogger<StupidStrategy>>();
        var strategy = new StupidStrategy(strategyLoggerMock.Object, options);
        var repositoryMock = new Mock<ISimulationRepository>();
        var runIdServiceMock = new Mock<RunIdService>();


        var testPhilosopher = new TestPhilosopher(
            PhilosopherName.Socrates,
            tableMock.Object,
            strategy,
            metricsMock.Object,
            options,
            philosopherLoggerMock.Object,
            repositoryMock.Object,
            runIdServiceMock.Object);

        return (testPhilosopher, strategy, tableMock);
    }


    [Fact]
    public async Task AcquireForks_Success_WhenBothAvailable()
    {

        // Arrange
        var (philosopher, strategy, tableMock) = CreatePhilosopher();
        
        tableMock.Setup(t => t.GetPhilosopherForks(philosopher.ExposedName)).Returns((0, 1));

        tableMock.Setup(t => t.WaitForForkAsync(
                            It.IsAny<int>(),
                            philosopher.ExposedName,
                            It.IsAny<CancellationToken>(),
                            It.IsAny<int?>()))
                 .ReturnsAsync(true);

        // Act
        var result = await strategy.TryAcquireForksAsync(philosopher.ExposedName, tableMock.Object, CancellationToken.None);

        // Assert
        Assert.True(result);

        tableMock.Verify(t => t.WaitForForkAsync(0, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);
        tableMock.Verify(t => t.WaitForForkAsync(1, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);

        tableMock.Verify(t => t.ReleaseFork(It.IsAny<int>(), philosopher.ExposedName), Times.Never);
    }

    [Fact]
    public async Task AcquireForks_Fails_WhenSecondForkUnavailable()
    {
        // Arrange

        var (philosopher, strategy, tableMock) = CreatePhilosopher();

        tableMock.Setup(t => t.GetPhilosopherForks(philosopher.ExposedName)).Returns((0, 1));

        tableMock.Setup(t => t.WaitForForkAsync(0, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                 .ReturnsAsync(true);     // левая доступна

        tableMock.Setup(t => t.WaitForForkAsync(1, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                 .ReturnsAsync(false);    // правая занята

        // Act
        var result = await strategy.TryAcquireForksAsync(philosopher.ExposedName, tableMock.Object, CancellationToken.None);

        // Assert
        Assert.False(result);

        tableMock.Verify(t => t.WaitForForkAsync(0, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);
        tableMock.Verify(t => t.WaitForForkAsync(1, philosopher.ExposedName, It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);

        
        tableMock.Verify(t => t.ReleaseFork(It.IsAny<int>(), philosopher.ExposedName), Times.Once);
    }
}
