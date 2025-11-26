// PhilosopherStateTransitionTests.cs
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Models.Enums;

namespace Philosophers.Tests;

public class PhilosopherStateTransitionTests
{
    /*
     * не в начале создаем отдельно кучу моков, а выносим все в метод с одним моком, 
     * потому что каждый тест 
     * вызывает моки,
     * настраивает моки и тд
     * А если моки общие - то состояние моков копится
     * и это может привести к "setup already exists" и тд
     * Поэтому проще просто каждый раз моки создавать
     */
    private (TestPhilosopher testPhilosopher, Mock<IPhilosopherStrategy> strategyMock) CreatePhilosopher(
        bool tryAcquireForksResult)
    {
        var tableMock = new Mock<ITableManager>();
        var strategyMock = new Mock<IPhilosopherStrategy>();
        var metricsMock = new Mock<IMetricsCollector>();
        var options = Options.Create(new SimulationOptions());

        strategyMock
            .Setup(s => s.TryAcquireForksAsync(
                It.IsAny<PhilosopherName>(),
                It.IsAny<ITableManager>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tryAcquireForksResult);

        var logger = new Mock<ILogger<TestPhilosopher>>();

        var testPhilosopher = new TestPhilosopher(
            PhilosopherName.Socrates,
            tableMock.Object,
            strategyMock.Object,
            metricsMock.Object,
            options,
            logger.Object);

        return (testPhilosopher, strategyMock);
    }



    [Fact]
    public async Task Thinking_To_Hungry()
    {
        // arrange
        var (philosopher, _) = CreatePhilosopher(tryAcquireForksResult: true);

        // act
        await philosopher.RunOneIteration(CancellationToken.None);

        // assert
        Assert.Equal(PhilosopherState.Hungry, philosopher.ExposedState);
    }

    [Fact]
    public async Task Hungry_To_Eating_When_Forks_Available()
    {
        var (philosopher, _) = CreatePhilosopher(tryAcquireForksResult: true);

        // переход в Hungry
        await philosopher.RunOneIteration(CancellationToken.None);

        // переход в Eating
        await philosopher.RunOneIteration(CancellationToken.None);

        Assert.Equal(PhilosopherState.Eating, philosopher.ExposedState);
    }

    [Fact]
    public async Task Hungry_Stays_Hungry_When_Forks_Not_Available()
    {
        var (philosopher, strategy) = CreatePhilosopher(tryAcquireForksResult: false);

        // Thinking => Hungry
        await philosopher.RunOneIteration(CancellationToken.None);

        // Hungry => Hungry (TryAcquireForksAsync = false)
        await philosopher.RunOneIteration(CancellationToken.None);

        Assert.Equal(PhilosopherState.Hungry, philosopher.ExposedState);
    }

    [Fact]
    public async Task Eating_To_Thinking()
    {        
        var (philosopher, strategy) = CreatePhilosopher(tryAcquireForksResult: true);

        // Thinking => Hungry => Eating
        await philosopher.RunOneIteration(CancellationToken.None);
        await philosopher.RunOneIteration(CancellationToken.None);

        // act
        // Eating => Thinking
        await philosopher.RunOneIteration(CancellationToken.None);

        Assert.Equal(PhilosopherState.Thinking, philosopher.ExposedState);

        // проверка, что ReleaseForks был вызван 1 раз
        strategy.Verify(s => s.ReleaseForks(
                PhilosopherName.Socrates,
                It.IsAny<ITableManager>()),
            Times.Once);
    }

    [Fact]
    public async Task Full_Cycle_Thinking_Hungry_Eating_Thinking()
    {
        var (philosopher, _) = CreatePhilosopher(tryAcquireForksResult: true);

        // Thinking => Hungry
        await philosopher.RunOneIteration(CancellationToken.None);
        Assert.Equal(PhilosopherState.Hungry, philosopher.ExposedState);

        // Hungry => Eating
        await philosopher.RunOneIteration(CancellationToken.None);
        Assert.Equal(PhilosopherState.Eating, philosopher.ExposedState);

        // Eating => Thinking
        await philosopher.RunOneIteration(CancellationToken.None);
        Assert.Equal(PhilosopherState.Thinking, philosopher.ExposedState);
    }
}


