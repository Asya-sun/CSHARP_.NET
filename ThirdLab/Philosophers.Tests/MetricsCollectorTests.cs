// MetricsCollectorTests.cs
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core;

namespace Philosophers.Tests;

public class MetricsCollectorTests
{

    public MetricsCollector CreateMetricsCollector()
    {
        var loggerMock = new Mock<ILogger<MetricsCollector>>();
        var options = Options.Create(new SimulationOptions());
        var metricsCollector = new MetricsCollector(loggerMock.Object, options);
        return metricsCollector;
    }

    [Fact]
    public void RecordEating_Should_Increment_Eat_Count()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        var philosopher = PhilosopherName.Socrates;

        // Act
        metricsCollector.RecordEating(philosopher);
        metricsCollector.RecordEating(philosopher);

        // Assert
        Assert.Equal( 2, metricsCollector.GetEatCount(philosopher));
    }

    [Fact]
    public void RecordWaitingTime_Should_Store_Waiting_Times()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        var philosopher = PhilosopherName.Plato;
        var waitingTime = TimeSpan.FromMilliseconds(150);

        // Act
        metricsCollector.RecordWaitingTime(philosopher, waitingTime);
        metricsCollector.RecordWaitingTime(philosopher, TimeSpan.FromMilliseconds(200));

        // Assert
        var waitingTimes = metricsCollector.GetWaitingTimes();
        Assert.True(waitingTimes.ContainsKey(philosopher));
        Assert.Equal(2, waitingTimes[philosopher].Count);
        Assert.Contains(waitingTime, waitingTimes[philosopher]);
    }

    [Fact]
    public void RecordThinkingTime_Should_Store_Thinking_Times()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        var philosopher = PhilosopherName.Aristotle;
        var thinkingTime = TimeSpan.FromMilliseconds(300);

        // Act
        metricsCollector.RecordThinkingTime(philosopher, thinkingTime);

        // Assert
        var thinkingTimes = metricsCollector.GetThinkingTimes();
        Assert.True(thinkingTimes.ContainsKey(philosopher));
        Assert.Single(thinkingTimes[philosopher]);
        Assert.Equal(thinkingTime, thinkingTimes[philosopher].First());
    }


    [Fact]
    public void RecordDeadlock_Should_Increment_Deadlock_Count()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();

        // Act
        metricsCollector.RecordDeadlock();
        metricsCollector.RecordDeadlock();

        // Assert
        Assert.Equal(2, metricsCollector.GetDeadlockCount());
    }

    [Fact]
    public void GetEatCounts_Should_Return_All_Philosophers_Counts()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        metricsCollector.RecordEating(PhilosopherName.Plato);
        metricsCollector.RecordEating(PhilosopherName.Plato);
        metricsCollector.RecordEating(PhilosopherName.Aristotle);

        // Act
        var eatCounts = metricsCollector.GetEatCounts();

        // Assert
        Assert.Equal(2, eatCounts[PhilosopherName.Plato]);
        Assert.Equal(1, eatCounts[PhilosopherName.Aristotle]);
        Assert.False(eatCounts.ContainsKey(PhilosopherName.Socrates));
    }


    [Fact]
    public void RecordForkAcquired_And_Released_Should_Calculate_Usage_Time()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        var forkId = 1;
        var philosopher = PhilosopherName.Socrates;

        // Act
        metricsCollector.RecordForkAcquired(forkId, philosopher);
        // Имитируем использование вилки
        Thread.Sleep(50);
        metricsCollector.RecordForkReleased(forkId);

        // Assert
        var forkUsage = metricsCollector.GetForkUsageTimes();
        Assert.True(forkUsage.ContainsKey(forkId));
        Assert.True(forkUsage[forkId].TotalMilliseconds > 0);
    }

    
    [Fact]
    public void ForkUsage_Should_Accumulate_Over_Multiple_Acquisitions()
    {
        // Arrange
        var metricsCollector = CreateMetricsCollector();
        var forkId = 2;

        // Act
        metricsCollector.RecordForkAcquired(forkId, PhilosopherName.Plato);
        Thread.Sleep(20);
        metricsCollector.RecordForkReleased(forkId);

        metricsCollector.RecordForkAcquired(forkId, PhilosopherName.Aristotle);
        Thread.Sleep(30);
        metricsCollector.RecordForkReleased(forkId);

        // Assert
        var forkUsage = metricsCollector.GetForkUsageTimes();
        var totalUsage = forkUsage[forkId].TotalMilliseconds;
        Assert.True(totalUsage >= 50);
    }
}