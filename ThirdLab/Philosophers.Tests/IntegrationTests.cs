using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Services;
using Philosophers.Strategies;
using Xunit.Abstractions;

namespace Philosophers.Tests;

public class IntegrationTests
{

    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // +
    [Fact]
    public async Task TableManagerAndStrategy_ShouldWorkTogether()
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

        // Act - Платон пытается взять вилки
        var result = await strategy.TryAcquireForksAsync("Платон", tableManager, CancellationToken.None);

        // Assert
        // Должен успешно взять обе вилки
        result.Should().BeTrue();
        tableManager.GetForkState(1).Should().Be(ForkState.InUse);
        tableManager.GetForkState(5).Should().Be(ForkState.InUse);
    }

    //+
    [Fact]
    public async Task PhilosopherLifecycle_ShouldCompleteFullCycle_WithRealComponents()
    {
        // Arrange - используем реальные компоненты (кроме логгера)
        var loggerMock = new Mock<ILogger<PhilosopherHostedService>>();
        var optionsMock = new Mock<IOptions<SimulationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
        {
            ThinkingTimeMin = 10,
            ThinkingTimeMax = 20,
            EatingTimeMin = 10,
            EatingTimeMax = 20,
            ForkAcquisitionTime = 5,
            DurationSeconds = 60
        });

        // Создаем реальный TableManager
        var tableManager = new TableManager(
            new Mock<ILogger<TableManager>>().Object,
            new Mock<IMetricsCollector>().Object);

        // НО Мокаем метод GetPhilosopherForks для тестового философа
        var tableManagerMock = new Mock<ITableManager>();
        tableManagerMock.Setup(t => t.GetPhilosopherForks("ТестовыйФилософ"))
                       .Returns((1, 2)); // Вилки 1 и 2 - они соседние и доступны

        // Остальные методы делегируем реальному tableManager
        tableManagerMock.Setup(t => t.TryAcquireForkAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .Returns<int, string, CancellationToken>((forkId, name, token) =>
                           tableManager.TryAcquireForkAsync(forkId, name, token));
        tableManagerMock.Setup(t => t.ReleaseFork(It.IsAny<int>(), It.IsAny<string>()))
                       .Callback<int, string>((forkId, name) => tableManager.ReleaseFork(forkId, name));
        tableManagerMock.Setup(t => t.GetForkState(It.IsAny<int>()))
                       .Returns<int>(forkId => tableManager.GetForkState(forkId));
        tableManagerMock.Setup(t => t.UpdatePhilosopherState(It.IsAny<string>(), It.IsAny<PhilosopherState>(), It.IsAny<string>()))
                       .Callback<string, PhilosopherState, string>((name, state, action) =>
                           tableManager.UpdatePhilosopherState(name, state, action));

        var strategy = new LeftRightStrategy(
            new Mock<ILogger<LeftRightStrategy>>().Object,
            optionsMock.Object);

        var metricsCollector = new MetricsCollector(
            new Mock<ILogger<MetricsCollector>>().Object,
            optionsMock.Object);

        // Создаем тестового философа
        var philosopher = new TestPhilosopher(
            "ТестовыйФилософ", tableManagerMock.Object, strategy, metricsCollector, optionsMock.Object, loggerMock.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Таймаут 5 секунд

        // Act - запускаем один цикл
        await philosopher.StartAsync(cts.Token);
        //  работает секунду
        await Task.Delay(1000);
        await philosopher.StopAsync(cts.Token);

        // Assert 
        var eatCount = metricsCollector.GetEatCount("ТестовыйФилософ");
        var thinkingTimes = metricsCollector.GetThinkingTimes();
        var waitingTimes = metricsCollector.GetWaitingTimes();
        var eatingTimes = metricsCollector.GetEatingTimes();

        // Философ должен был и подумать, и поесть
        thinkingTimes.Should().ContainKey("ТестовыйФилософ");
        thinkingTimes["ТестовыйФилософ"].Should().NotBeEmpty();

        eatingTimes.Should().ContainKey("ТестовыйФилософ");
        eatingTimes["ТестовыйФилософ"].Should().NotBeEmpty();
        waitingTimes.Should().ContainKey("ТестовыйФилософ");

        // Философ должен был поесть хотя бы 1 раз
        eatCount.Should().BeGreaterThan(0);

        _output.WriteLine($"Философ поел: {eatCount} раз");
        _output.WriteLine($"Периодов мышления: {thinkingTimes["ТестовыйФилософ"].Count}");
        _output.WriteLine($"Периодов ожидания: {waitingTimes["ТестовыйФилософ"].Count}");
        _output.WriteLine($"Периодов еды: {eatingTimes["ТестовыйФилософ"].Count}");
    }


    

    // мб в тесты для display service - ??
    [Fact]
    public async Task DisplayService_ShouldWorkWithoutErrors()
    {
        // Arrange
        var tableManagerMock = new Mock<ITableManager>();
        var metricsCollectorMock = new Mock<IMetricsCollector>();
        var optionsMock = new Mock<IOptions<SimulationOptions>>();
        var loggerMock = new Mock<ILogger<DisplayService>>();

        optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
        {
            DisplayUpdateInterval = 10
        });

        var displayService = new DisplayService(
            tableManagerMock.Object, metricsCollectorMock.Object, optionsMock.Object, loggerMock.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        var act = async () => await displayService.StartAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ShouldDemonstrateDeadlock_WithDirectTableManagerAccess()
    {
        // Arrange
        var tableManager = new TableManager(
            new Mock<ILogger<TableManager>>().Object,
            new Mock<IMetricsCollector>().Object);

        // Act - СОЗДАЕМ DEADLOCK ВРУЧНУЮ
        // Все философы берут левые вилки одновременно

        var tasks = new[]
        {
        // Платон берет вилку 1 (левую)
        tableManager.TryAcquireForkAsync(1, "Платон", CancellationToken.None),
        
        // Аристотель берет вилку 2 (левую)  
        tableManager.TryAcquireForkAsync(2, "Аристотель", CancellationToken.None),
        
        // Сократ берет вилку 3 (левую)
        tableManager.TryAcquireForkAsync(3, "Сократ", CancellationToken.None),
        
        // Декарт берет вилку 4 (левую)
        tableManager.TryAcquireForkAsync(4, "Декарт", CancellationToken.None),
        
        // Кант берет вилку 5 (левую)
        tableManager.TryAcquireForkAsync(5, "Кант", CancellationToken.None)
    };

        // Ждем пока все возьмут левые вилки
        var leftForksAcquired = await Task.WhenAll(tasks);
        leftForksAcquired.Should().AllBeEquivalentTo(true, "Все должны получить левые вилки");

        // Теперь пытаемся взять правые вилки - ДОЛЖЕН ВОЗНИКНУТЬ DEADLOCK
        var rightForkTasks = new[]
        {
        // Платон пытается взять вилку 5 (правую) - занята Кантом
        tableManager.TryAcquireForkAsync(5, "Платон", CancellationToken.None),
        
        // Аристотель пытается взять вилку 1 (правую) - занята Платоном  
        tableManager.TryAcquireForkAsync(1, "Аристотель", CancellationToken.None),
        
        // Сократ пытается взять вилку 2 (правую) - занята Аристотелем
        tableManager.TryAcquireForkAsync(2, "Сократ", CancellationToken.None),
        
        // Декарт пытается взять вилку 3 (правую) - занята Сократом
        tableManager.TryAcquireForkAsync(3, "Декарт", CancellationToken.None),
        
        // Кант пытается взять вилку 4 (правую) - занята Декартом
        tableManager.TryAcquireForkAsync(4, "Кант", CancellationToken.None)
    };

        // Assert - проверяем что возникает deadlock (зависание)
        var completionTask = Task.WhenAll(rightForkTasks);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));

        var firstCompleted = await Task.WhenAny(completionTask, timeoutTask);

        // Если первым завершился timeout - значит DEADLOCK!
        firstCompleted.Should().Be(timeoutTask, "Система должна зависнуть в deadlock при попытке взять правые вилки");

        // Освобождаем вилки чтобы не влиять на другие тесты
        foreach (var philosopher in new[] { "Платон", "Аристотель", "Сократ", "Декарт", "Кант" })
        {
            for (int i = 1; i <= 5; i++)
            {
                tableManager.ReleaseFork(i, philosopher);
            }
        }
    }
}

// Test класс для философа чтобы можно было тестировать
public class TestPhilosopher : PhilosopherHostedService
{
    public TestPhilosopher(
        string name, ITableManager tableManager, IPhilosopherStrategy strategy,
        IMetricsCollector metricsCollector, IOptions<SimulationOptions> options,
        ILogger<PhilosopherHostedService> logger)
        : base(name, tableManager, strategy, metricsCollector, options, logger)
    {
    }

    // Делаем внутренние методы доступными для тестирования
    public new Task<bool> TryEat(CancellationToken cancellationToken) => base.TryEat(cancellationToken);
    public new Task Think(CancellationToken cancellationToken) => base.Think(cancellationToken);
    public new Task Eat(CancellationToken cancellationToken) => base.Eat(cancellationToken);
}