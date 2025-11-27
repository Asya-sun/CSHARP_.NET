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
using System.Xml.Linq;

namespace Philosophers.Tests;

public class DeadlockTests
{
    private (TestDeadlockDetector detector, Mock<ITableManager> tableMock) CreateTestDeadlockDetector()
    {
        var tableMock = new Mock<ITableManager>();
        var metricsMock = new Mock<IMetricsCollector>();
        var loggerMock = new Mock<ILogger<TestDeadlockDetector>>();
        var repositoryMock = new Mock<ISimulationRepository>();
        var runIdServiceMock = new Mock<RunIdService>();

        var detector = new TestDeadlockDetector(
            tableMock.Object,
            loggerMock.Object,
            metricsMock.Object,
            repositoryMock.Object,
            runIdServiceMock.Object);
        return (detector, tableMock);
    }

    private void SetupDeadlockConditions(Mock<ITableManager> tableMock)
    {
        var philosophers = new List<Philosopher>
        {
            new Philosopher(PhilosopherName.Plato) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Aristotle) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Socrates) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Decartes) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Kant) { State = PhilosopherState.Hungry }
        };

        var forks = new List<Fork>
        {
            new Fork(1) { _state = ForkState.InUse, _usedBy = PhilosopherName.Plato },
            new Fork(2) { _state = ForkState.InUse, _usedBy = PhilosopherName.Aristotle },
            new Fork(3) { _state = ForkState.InUse, _usedBy = PhilosopherName.Socrates },
            new Fork(4) { _state = ForkState.InUse, _usedBy = PhilosopherName.Decartes },
            new Fork(5) { _state = ForkState.InUse, _usedBy = PhilosopherName.Kant }
        };

        tableMock.Setup(t => t.GetAllPhilosophers()).Returns(philosophers);
        tableMock.Setup(t => t.GetAllForks()).Returns(forks);

        // Настраиваем вызов ReleaseFork для любого философа и любой вилки
        tableMock.Setup(t => t.ReleaseFork(It.IsAny<int>(), It.IsAny<PhilosopherName>()));
    }

    [Fact]
    public void CheckForDeadlock_Should_Return_True_When_All_Philosophers_Hungry_And_All_Forks_InUse()
    {
        // Arrange
        var (testDeadlockDetector, tableMock) = CreateTestDeadlockDetector();
        SetupDeadlockConditions(tableMock);

        // Act
        var result = testDeadlockDetector.CheckForDeadlock();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CheckForDeadlock_Should_Return_False_When_Not_All_Philosophers_Hungry()
    {
        // Arrange
        var (testDeadlockDetector, tableMock) = CreateTestDeadlockDetector();
        var philosophers = new List<Philosopher>
        {
            new Philosopher(PhilosopherName.Plato) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Aristotle) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Socrates) { State = PhilosopherState.Eating },
            new Philosopher(PhilosopherName.Decartes) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Kant) { State = PhilosopherState.Hungry }
        };

        var forks = new List<Fork>
        {
            new Fork(1) { _state = ForkState.InUse, _usedBy = PhilosopherName.Plato },
            new Fork(2) { _state = ForkState.InUse, _usedBy = PhilosopherName.Aristotle },
            new Fork(3) { _state = ForkState.InUse, _usedBy = PhilosopherName.Socrates },
            new Fork(4) { _state = ForkState.InUse, _usedBy = PhilosopherName.Decartes },
            new Fork(5) { _state = ForkState.InUse, _usedBy = PhilosopherName.Kant }
        };

        tableMock.Setup(t => t.GetAllPhilosophers()).Returns(philosophers);
        tableMock.Setup(t => t.GetAllForks()).Returns(forks);

        // Act
        var result = testDeadlockDetector.CheckForDeadlock();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteOneCheckCycle_Should_Resolve_Deadlock_When_Detected()
    {
        // Arrange
        var (testDeadlockDetector, tableMock) = CreateTestDeadlockDetector();
        SetupDeadlockConditions(tableMock);

        // Act
        await testDeadlockDetector.ExecuteOneCheckCycle();

        // Assert
        Assert.True(testDeadlockDetector.WasDeadlockResolved);
        Assert.Equal(1, testDeadlockDetector.DeadlockResolutionCount);

        // ??
        tableMock.Verify(t => t.ReleaseFork(It.IsAny<int>(), It.IsAny<PhilosopherName>()), Times.Exactly(2) );
        //tableMock.Verify(t => t.ReleaseFork(It.IsAny<int>(), It.IsAny<PhilosopherName>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteOneCheckCycle_Should_Not_Resolve_When_No_Deadlock()
    {
        // Arrange
        var (testDeadlockDetector, tableMock) = CreateTestDeadlockDetector();

        var philosophers = new List<Philosopher>
        {
            new Philosopher(PhilosopherName.Plato) { State = PhilosopherState.Thinking },
            new Philosopher(PhilosopherName.Aristotle) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Socrates) { State = PhilosopherState.Eating },
            new Philosopher(PhilosopherName.Decartes) { State = PhilosopherState.Hungry },
            new Philosopher(PhilosopherName.Kant) { State = PhilosopherState.Thinking }
        };

        var forks = new List<Fork>
        {
            new Fork(1) { _state = ForkState.Available },
            new Fork(2) { _state = ForkState.InUse, _usedBy = PhilosopherName.Aristotle },
            new Fork(3) { _state = ForkState.InUse, _usedBy = PhilosopherName.Socrates },
            new Fork(4) { _state = ForkState.InUse, _usedBy = PhilosopherName.Decartes },
            new Fork(5) { _state = ForkState.Available }
        };

        tableMock.Setup(t => t.GetAllPhilosophers()).Returns(philosophers);
        tableMock.Setup(t => t.GetAllForks()).Returns(forks);

        // Act
        await testDeadlockDetector.ExecuteOneCheckCycle();

        // Assert
        Assert.False(testDeadlockDetector.WasDeadlockResolved);
        Assert.Equal(0, testDeadlockDetector.DeadlockResolutionCount);
    }

}


public class TestDeadlockDetector : DeadlockDetector
{
    public bool WasDeadlockResolved { get; private set; } = false;
    public int DeadlockResolutionCount { get; private set; } = 0;

    public TestDeadlockDetector(
        ITableManager tableManager,
        ILogger<TestDeadlockDetector> logger,
        IMetricsCollector metricsCollector,
        ISimulationRepository repository,
        RunIdService runIdService)
        : base(tableManager, logger, metricsCollector, repository, runIdService)
    {
    }

    public async Task ExecuteOneCheckCycle(CancellationToken cancellationToken = default)
    {
        WasDeadlockResolved = false;

        if (CheckForDeadlock())
        {
            await ResolveDeadlock();
            WasDeadlockResolved = true;
            DeadlockResolutionCount++;
        }
    }

    // для прямого вызова разрешения дедлока в тестах
    public async Task ForceResolveDeadlock()
    {
        await ResolveDeadlock();
    }
}
