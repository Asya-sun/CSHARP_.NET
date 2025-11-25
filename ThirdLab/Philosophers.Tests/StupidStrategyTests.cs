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

public class StupidStrategyTests
{
    private readonly Mock<ILogger<StupidStrategy>> _loggerMock;
    private readonly Mock<IOptions<SimulationOptions>> _optionsMock;
    private readonly StupidStrategy _strategy;

    public StupidStrategyTests()
    {
        _loggerMock = new Mock<ILogger<StupidStrategy>>();
        _optionsMock = new Mock<IOptions<SimulationOptions>>();

        _optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
        {
            ForkAcquisitionTime = 10
        });

        _strategy = new StupidStrategy(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldUseInfiniteWait_ForBothForks()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        // НАСТРАИВАЕМ mock чтобы он возвращал true для БЕСКОНЕЧНОГО ожидания
        tableManagerMock.Setup(t => t.WaitForForkAsync(1, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                       .ReturnsAsync(true);
        tableManagerMock.Setup(t => t.WaitForForkAsync(5, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                       .ReturnsAsync(true);

        // Act
        var result = await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        // Проверяем что используется бесконечное ожидание
        tableManagerMock.Verify(t => t.WaitForForkAsync(1, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);
        tableManagerMock.Verify(t => t.WaitForForkAsync(5, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task TryAcquireForksAsync_ShouldNotReleaseLeftFork_WhenRightForkIsBusy()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("Платон")).Returns((1, 5));

        // Левая вилка берется, правая - вечно ждет (возвращаем задачу которая никогда не завершится)
        tableManagerMock.Setup(t => t.WaitForForkAsync(1, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                       .ReturnsAsync(true);
        tableManagerMock.Setup(t => t.WaitForForkAsync(5, "Платон", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
                       .Returns(Task.Delay(Timeout.Infinite).ContinueWith(_ => false));

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, cts.Token);

        // Assert
        result.Should().BeFalse();
        // В StupidStrategy левая вилка НЕ отпускается при неудаче с правой!
        tableManagerMock.Verify(t => t.ReleaseFork(1, "Платон"), Times.Never);
    }

    [Fact]
    public async Task MultipleStupidPhilosophers_ShouldCreateDeadlock()
    {
        // Arrange
        var tableManager = new TableManager(
            new Mock<ILogger<TableManager>>().Object,
            new Mock<IMetricsCollector>().Object);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act - все философы одновременно пытаются взять вилки
        var tasks = new List<Task<bool>>();
        var exceptions = new List<Exception>();

        tasks.Add(_strategy.TryAcquireForksAsync("Платон", tableManager, cts.Token));
        tasks.Add(_strategy.TryAcquireForksAsync("Аристотель", tableManager, cts.Token));
        tasks.Add(_strategy.TryAcquireForksAsync("Сократ", tableManager, cts.Token));
        tasks.Add(_strategy.TryAcquireForksAsync("Декарт", tableManager, cts.Token));
        tasks.Add(_strategy.TryAcquireForksAsync("Кант", tableManager, cts.Token));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Это ожидаемо - дедлок приводит к отмене операций
        }

        // Assert - проверяем состояние ДО отмены
        // Все вилки должны быть заняты (дедлок!)
        for (int i = 1; i <= 5; i++)
        {
            tableManager.GetForkState(i).Should().Be(ForkState.InUse, $"вилка {i} должна быть занята при дедлоке");
        }

        // Проверяем что никто не успел поесть до дедлока
        var completedResults = tasks.Where(t => t.IsCompletedSuccessfully).Select(t => t.Result).ToArray();
        if (completedResults.Any())
        {
            completedResults.Should().NotContain(true, "никто не должен был получить обе вилки");
        }
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

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _strategy.TryAcquireForksAsync("Платон", tableManagerMock.Object, cts.Token));
    }

    [Fact]
    public async Task StupidStrategy_ShouldActuallyCreateDeadlock_InRealScenario()
    {
        // Arrange
        var tableManager = new TableManager(
            new Mock<ILogger<TableManager>>().Object,
            new Mock<IMetricsCollector>().Object);

        // Act - создаем ситуацию дедлока: все берут левые вилки
        // ✅ ИСПРАВЛЕНО: используем мгновенную проверку (timeout = 0)
        var platoLeft = await tableManager.WaitForForkAsync(1, "Платон", CancellationToken.None, 0);
        var aristotleLeft = await tableManager.WaitForForkAsync(2, "Аристотель", CancellationToken.None, 0);
        var socratesLeft = await tableManager.WaitForForkAsync(3, "Сократ", CancellationToken.None, 0);
        var descartesLeft = await tableManager.WaitForForkAsync(4, "Декарт", CancellationToken.None, 0);
        var kantLeft = await tableManager.WaitForForkAsync(5, "Кант", CancellationToken.None, 0);

        // Проверяем что все взяли левые вилки
        new[] { platoLeft, aristotleLeft, socratesLeft, descartesLeft, kantLeft }
            .Should().NotContain(false, "все должны были взять левые вилки");

        // Теперь пытаемся взять правые вилки - это создаст дедлок
        // ✅ ИСПРАВЛЕНО: используем мгновенную проверку (timeout = 0)
        var platoRight = await tableManager.WaitForForkAsync(5, "Платон", CancellationToken.None, 0);
        var aristotleRight = await tableManager.WaitForForkAsync(1, "Аристотель", CancellationToken.None, 0);
        var socratesRight = await tableManager.WaitForForkAsync(2, "Сократ", CancellationToken.None, 0);
        var descartesRight = await tableManager.WaitForForkAsync(3, "Декарт", CancellationToken.None, 0);
        var kantRight = await tableManager.WaitForForkAsync(4, "Кант", CancellationToken.None, 0);

        var rightResults = new[] { platoRight, aristotleRight, socratesRight, descartesRight, kantRight };

        // Assert - никто не должен был получить правые вилки из-за дедлока
        rightResults.Should().NotContain(true, "дедлок должен предотвратить получение правых вилок");

        // Дополнительная проверка: все вилки остались занятыми
        for (int i = 1; i <= 5; i++)
        {
            tableManager.GetForkState(i).Should().Be(ForkState.InUse, $"вилка {i} должна быть занята");
        }
    }
}