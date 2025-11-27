using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Moq;
using Philosophers.Core.Models.Enums;
using Philosophers.DB.Context;
using Philosophers.DB.Entities;
using Philosophers.DB.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Philosophers.Tests;

public class SimulationRepositoryDbTests : IDisposable
{
    private SqliteConnection _connection;

    private (DbContextOptions<SimulationDBContext> options, IDbContextFactory<SimulationDBContext> factory) CreateInMemoryContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SimulationDBContext>()
            .UseSqlite(_connection)
            .Options;

        // Создаем базу с тестовым контекстом
        using var context = new TestSimulationDBContext(options);
        context.Database.EnsureCreated();

        var factoryMock = new Mock<IDbContextFactory<SimulationDBContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(() => new TestSimulationDBContext(options));

        return (options, factoryMock.Object);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private (DbContextOptions<SimulationDBContext> options,
        IDbContextFactory<SimulationDBContext> factory,
        Mock<ILogger<SimulationRepository>> loggerMock,
        SimulationRepository repository)
        PrepareStuffForTests()
    {
        var (options, factory) = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<SimulationRepository>>();
        var repository = new SimulationRepository(factory, loggerMock.Object);
        return (options, factory, loggerMock, repository);
    }

    // проверка сохранения вилки в бд
    [Fact]
    public async Task RecordForkStateAsync_ShouldSaveForkState()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        // подготавливаем данные с использованием TestSimulationDBContext
        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });
        await setupContext.SaveChangesAsync();

        await repository.RecordForkStateAsync(
            runId, 1, ForkState.InUse, PhilosopherName.Plato, TimeSpan.FromSeconds(10));

        using var assertContext = new TestSimulationDBContext(options);
        var forkState = await assertContext.ForkStateChanges.FirstAsync();
        Assert.Equal(1, forkState.ForkId);
        Assert.Equal(ForkState.InUse, forkState.State);
        Assert.Equal(PhilosopherName.Plato, forkState.UsedBy);
    }

    // проверка, что получаем правильное состояние вилки для заданного времени
    [Fact]
    public async Task GetForkStatesAtTimeAsync_ShouldReturnCorrectStates()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });

        // Добавляем несколько состояний вилки с разным временем
        setupContext.ForkStateChanges.AddRange(
            new ForkStateChange
            {
                RunId = runId,
                ForkId = 1,
                State = ForkState.Available,
                UsedBy = null,
                SimulationTime = TimeSpan.FromSeconds(5),
                Timestamp = DateTime.UtcNow
            },
            new ForkStateChange
            {
                RunId = runId,
                ForkId = 1,
                State = ForkState.InUse,
                UsedBy = PhilosopherName.Plato,
                SimulationTime = TimeSpan.FromSeconds(15),
                Timestamp = DateTime.UtcNow.AddSeconds(1)
            }
        );
        await setupContext.SaveChangesAsync();

        var statesAtTime10 = await repository.GetForkStatesAtTimeAsync(runId, TimeSpan.FromSeconds(10));

        Assert.NotEmpty(statesAtTime10);
        var state = statesAtTime10.Single(f => f.ForkId == 1);
        Assert.Equal(ForkState.Available, state.State);
    }

    // проверяем, что сохраняет инфу о дедлоке в бд
    [Fact]
    public async Task RecordDeadlockAsync_ShouldSaveDeadlock()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });
        await setupContext.SaveChangesAsync();

        await repository.RecordDeadlockAsync(
            runId, 1, TimeSpan.FromSeconds(10), PhilosopherName.Aristotle);

        using var assertContext = new TestSimulationDBContext(options);
        var deadlock = await assertContext.DeadlockRecords.FirstAsync();
        Assert.Equal(1, deadlock.DeadlockNumber);
        Assert.Equal(PhilosopherName.Aristotle, deadlock.ResolvedByPhilosopher);
    }

    // проверяем получения спска всех дедлоков для runId
    [Fact]
    public async Task GetDeadlocksAsync_ShouldReturnDeadlocks()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });

        setupContext.DeadlockRecords.AddRange(
            new DeadlockRecord
            {
                RunId = runId,
                DeadlockNumber = 1,
                SimulationTime = TimeSpan.FromSeconds(5),
                ResolvedByPhilosopher = PhilosopherName.Plato
            },
            new DeadlockRecord
            {
                RunId = runId,
                DeadlockNumber = 2,
                SimulationTime = TimeSpan.FromSeconds(15),
                ResolvedByPhilosopher = PhilosopherName.Aristotle
            }
        );
        await setupContext.SaveChangesAsync();

        var deadlocks = await repository.GetDeadlocksAsync(runId);

        Assert.Equal(2, deadlocks.Count);
        Assert.Equal(1, deadlocks[0].DeadlockNumber);
        Assert.Equal(2, deadlocks[1].DeadlockNumber);
    }

    // проверяем, что получаем инфу о запуске симуляции по runId
    [Fact]
    public async Task GetRunAsync_ShouldReturnRun()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();
        var expectedRun = new SimulationRun
        {
            RunId = runId,
            StartedAt = DateTime.UtcNow,
            OptionsJson = "{\"DurationSeconds\":60}"
        };

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(expectedRun);
        await setupContext.SaveChangesAsync();

        var result = await repository.GetRunAsync(runId);

        Assert.NotNull(result);
        Assert.Equal(runId, result.RunId);
        Assert.Equal(expectedRun.OptionsJson, result.OptionsJson);
    }

    // проверяем уставноку времени завершения симуляции
    [Fact]
    public async Task CompleteRunAsync_ShouldSetFinishedAt()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });
        await setupContext.SaveChangesAsync();

        await repository.CompleteRunAsync(runId);

        using var assertContext = new TestSimulationDBContext(options);
        var run = await assertContext.SimulationRuns.FirstAsync();
        Assert.NotNull(run.FinishedAt);
    }

    // проверяем, что сохраняет инфу о философе в бд
    [Fact]
    public async Task RecordPhilosopherStateAsync_ShouldSavePhilosopherState()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });
        await setupContext.SaveChangesAsync();

        // Act
        await repository.RecordPhilosopherStateAsync(
            runId, PhilosopherName.Plato, PhilosopherState.Thinking, "ReleaseLeftFork|ReleaseRightFork", "StupidStrategy", TimeSpan.FromSeconds(5));

        // Assert
        using var assertContext = new TestSimulationDBContext(options);
        var philosopherState = await assertContext.PhilosopherStateChanges.FirstAsync();
        Assert.Equal(PhilosopherName.Plato, philosopherState.PhilosopherName);
        Assert.Equal(PhilosopherState.Thinking, philosopherState.State);
        Assert.Equal("ReleaseLeftFork|ReleaseRightFork", philosopherState.Action);
        Assert.Equal("StupidStrategy", philosopherState.StrategyName);
        Assert.Equal(TimeSpan.FromSeconds(5), philosopherState.SimulationTime);
    }

    // проверка, что получаем правильное состояние философа для заданного времени
    [Fact]
    public async Task GetPhilosopherStatesAtTimeAsync_ShouldReturnCorrectStates()
    {
        var (options, factory, loggerMock, repository) = PrepareStuffForTests();

        var runId = Guid.NewGuid();

        using var setupContext = new TestSimulationDBContext(options);
        setupContext.SimulationRuns.Add(new SimulationRun { RunId = runId, StartedAt = DateTime.UtcNow });

        // Добавляем несколько состояний философа с разным временем
        setupContext.PhilosopherStateChanges.AddRange(
            new PhilosopherStateChange
            {
                RunId = runId,
                PhilosopherName = PhilosopherName.Plato,
                State = PhilosopherState.Thinking,
                Action = "ReleaseLeftFork|ReleaseRightFork",
                StrategyName = "StupidStrategy",
                SimulationTime = TimeSpan.FromSeconds(5),
                Timestamp = DateTime.UtcNow
            },
            new PhilosopherStateChange
            {
                RunId = runId,
                PhilosopherName = PhilosopherName.Plato,
                State = PhilosopherState.Hungry,
                Action = "TakeLeftFork|TakeRightFork",
                StrategyName = "StupidStrategy",
                SimulationTime = TimeSpan.FromSeconds(15),
                Timestamp = DateTime.UtcNow.AddSeconds(1)
            }
        );
        await setupContext.SaveChangesAsync();

        // Act - Запрашиваем состояние на 10 секунде
        var statesAtTime10 = await repository.GetPhilosopherStatesAtTimeAsync(runId, TimeSpan.FromSeconds(10));

        // Assert - Должен вернуться Thinking (состояние на 5 секунде)
        Assert.NotEmpty(statesAtTime10); // ДОБАВЬ ЭТУ ПРОВЕРКУ
        var state = statesAtTime10.Single(p => p.PhilosopherName == PhilosopherName.Plato);
        Assert.Equal(PhilosopherState.Thinking, state.State);
        Assert.Equal("ReleaseLeftFork|ReleaseRightFork", state.Action);
    }
}

public class TestSimulationDBContext : SimulationDBContext
{
    public TestSimulationDBContext(DbContextOptions<SimulationDBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Используем правильный конвертер для TimeSpan
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(TimeSpan) || property.ClrType == typeof(TimeSpan?))
                {
                    property.SetValueConverter(
                        new ValueConverter<TimeSpan, long>(
                            v => v.Ticks,
                            v => TimeSpan.FromTicks(v)
                        )
                    );
                }
            }
        }
    }
}