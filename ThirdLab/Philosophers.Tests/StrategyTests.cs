using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Services;
using Philosophers.Strategies;
using Philosophers.Core.Models;
using Microsoft.Extensions.Options;

namespace Philosophers.Tests;

public class StrategyTests
{

    private readonly Mock<ILogger<LeftRightStrategy>> _loggerMock;
    private readonly Mock<IOptions<SimulationOptions>> _optionsMock;
    private readonly LeftRightStrategy _strategy;

    public StrategyTests()
    {
        _loggerMock = new Mock<ILogger<LeftRightStrategy>>();
        _optionsMock = new Mock<IOptions<SimulationOptions>>();

        _optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
        {
            ForkAcquisitionTime = 10 // Короткое время для тестов
        });

        _strategy = new LeftRightStrategy(_loggerMock.Object, _optionsMock.Object);
    }

   
    [Fact]
    public async Task TryAcquireForksAsync_ShouldAcquireBothForks_WhenBothAvailable()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        tableManagerMock.SetupSequence(t => t.TryAcquireForkAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true)  // Левая вилка
                       .ReturnsAsync(true); // Правая вилка

        // Act
        var result = await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        tableManagerMock.Verify(t => t.TryAcquireForkAsync(1, "Платон", It.IsAny<CancellationToken>()), Times.Once);
        tableManagerMock.Verify(t => t.TryAcquireForkAsync(5, "Платон", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldFail_WhenLeftForkIsBusy()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        // Левая вилка занята
        tableManagerMock.Setup(t => t.TryAcquireForkAsync(1, "Платон", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

        // Act
        var result = await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        tableManagerMock.Verify(t => t.TryAcquireForkAsync(1, "Платон", It.IsAny<CancellationToken>()), Times.Once);
        tableManagerMock.Verify(t => t.TryAcquireForkAsync(5, "Платон", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldReleaseLeftFork_WhenRightForkIsBusy()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        tableManagerMock.SetupSequence(t => t.TryAcquireForkAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true)   // Левая вилка успешно
                       .ReturnsAsync(false); // Правая вилка занята

        // Act
        var result = await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        // Проверяем что левая вилка была освобождена (откат)
        tableManagerMock.Verify(t => t.ReleaseFork(1, "Платон"), Times.Once);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldAcquireForksInCorrectOrder_LeftThenRight()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        var callOrder = new List<int>();
        tableManagerMock.Setup(t => t.TryAcquireForkAsync(1, "Платон", It.IsAny<CancellationToken>()))
                       .Callback<int, string, CancellationToken>((id, name, token) => callOrder.Add(id))
                       .ReturnsAsync(true);
        tableManagerMock.Setup(t => t.TryAcquireForkAsync(5, "Платон", It.IsAny<CancellationToken>()))
                       .Callback<int, string, CancellationToken>((id, name, token) => callOrder.Add(id))
                       .ReturnsAsync(true);

        // Act
        await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, CancellationToken.None);

        // Assert
        callOrder.Should().ContainInOrder(1, 5); // Сначала левая (1), потом правая (5)
    }

    [Fact]
    public void ReleaseForks_ShouldReleaseBothForks()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        // Act
        _strategy.ReleaseForks("Платон", tableManagerMock.Object);

        // Assert
        tableManagerMock.Verify(t => t.ReleaseFork(1, "Платон"), Times.Once);
        tableManagerMock.Verify(t => t.ReleaseFork(5, "Платон"), Times.Once);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Сразу отменяем


        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, cts.Token));
        
        // Проверяем что не было попыток взять вилки при отмененном токене
        tableManagerMock.Verify(t => t.TryAcquireForkAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MultiplePhilosophers_ShouldNotDeadlock()
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

        var strategy = new LeftRightStrategy(
            new Mock<ILogger<LeftRightStrategy>>().Object,
             optionsMock.Object);

        // Act - несколько философов одновременно пытаются взять вилки
        var tasks = new[]
        {
            strategy.TryAcquireForksAsync("Платон", tableManager, CancellationToken.None),
            strategy.TryAcquireForksAsync("Аристотель", tableManager, CancellationToken.None),
            strategy.TryAcquireForksAsync("Сократ", tableManager, CancellationToken.None)
        };

        var results = await Task.WhenAll(tasks);

        // Assert - хотя бы один должен был получить вилки
        // Кто-то должен был поесть
        // И не должно быть deadlock (тест завершился)
        results.Should().Contain(true);
    }


    //[Fact]
    //public async Task Strategy_ShouldComplete_WithinReasonableTime_NoDeadlock()
    //{
    //    // Arrange
    //    var tableManager = new TableManager(
    //        new Mock<ILogger<TableManager>>().Object,
    //        new Mock<IMetricsCollector>().Object);

    //    var optionsMock = new Mock<IOptions<SimulationOptions>>();
    //    optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
    //    {
    //        ForkAcquisitionTime = 10
    //    });

    //    var strategy = new LeftRightStrategy(
    //        new Mock<ILogger<LeftRightStrategy>>().Object,
    //        optionsMock.Object);

    //    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Таймаут 3 секунды

    //    // Act - все философы пытаются взять вилки
    //    var tasks = new[]
    //    {
    //    strategy.TryAcquireForksAsync("Платон", tableManager, cts.Token),
    //    strategy.TryAcquireForksAsync("Аристотель", tableManager, cts.Token),
    //    strategy.TryAcquireForksAsync("Сократ", tableManager, cts.Token),
    //    strategy.TryAcquireForksAsync("Декарт", tableManager, cts.Token),
    //    strategy.TryAcquireForksAsync("Кант", tableManager, cts.Token)
    //};

    //    // Assert - операция должна завершиться ДО таймаута (нет deadlock)
    //    var completionTask = Task.WhenAll(tasks);
    //    var completedTask = await Task.WhenAny(completionTask, Task.Delay(5000));

    //    completedTask.Should().Be(completionTask, "Операция не должна зависать в deadlock");

    //    // Если дошли сюда - deadlock нет!
    //    var results = await completionTask;
    //    results.Should().Contain(true, "Хотя бы один философ должен получить вилки");
    //}
}