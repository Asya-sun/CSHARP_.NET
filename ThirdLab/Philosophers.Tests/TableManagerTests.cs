using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models.Enums;
using Philosophers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Tests
{
    public class TableManagerTests
    {

        private readonly Mock<ILogger<TableManager>> _loggerMock;
        private readonly Mock<IMetricsCollector> _metricsMock;
        private readonly TableManager _tableManager;

        public TableManagerTests()
        {
            _loggerMock = new Mock<ILogger<TableManager>>();
            _metricsMock = new Mock<IMetricsCollector>();
            _tableManager = new TableManager(_loggerMock.Object, _metricsMock.Object);
        }

        [Fact]
        public async Task TryAcquireForkAsync_ShouldAcquireWhenAvailable()
        {
            // Arrange
            var forkId = 1;
            var philosopher = "Платон";

            // Act
            var result = await _tableManager.TryAcquireForkAsync(forkId, philosopher, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _tableManager.GetForkOwner(forkId).Should().Be(philosopher);
        }

        [Fact]
        public async Task TryAcquireForkAsync_ShouldFailWhenForkIsBusy()
        {
            // Arrange
            var forkId = 1;

            await _tableManager.TryAcquireForkAsync(forkId, "Аристотель", CancellationToken.None);

            // Act
            var result = await _tableManager.TryAcquireForkAsync(forkId, "Платон", CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetPhilosopherForks_ShouldReturnCorrectForks()
        {
            // Act & Assert для всех философов
            _tableManager.GetPhilosopherForks("Платон").Should().Be((1, 5));
            _tableManager.GetPhilosopherForks("Аристотель").Should().Be((2, 1));
            _tableManager.GetPhilosopherForks("Сократ").Should().Be((3, 2));
            _tableManager.GetPhilosopherForks("Декарт").Should().Be((4, 3));
            _tableManager.GetPhilosopherForks("Кант").Should().Be((5, 4));
        }


        [Fact]
        public void ReleaseFork_ShouldMakeForkAvailable()
        {
            // Arrange
            var forkId = 1;

            // Платон берет вилку
            _tableManager.TryAcquireForkAsync(forkId, "Платон", CancellationToken.None).Wait();
            _tableManager.GetForkState(forkId).Should().Be(ForkState.InUse); // Проверяем что взята

            // Act
            _tableManager.ReleaseFork(forkId, "Платон");

            // Assert
            _tableManager.GetForkState(forkId).Should().Be(ForkState.Available);
            _tableManager.GetForkOwner(forkId).Should().BeNull();
        }


        [Fact]
        public void GetAdjacentForksState_ShouldReturnCorrectStates()
        {
            // Arrange
            // Платон берет свою левую вилку (1)
            _tableManager.TryAcquireForkAsync(1, "Платон", CancellationToken.None).Wait();

            // Act
            var (leftState, rightState) = _tableManager.GetAdjacentForksState("Платон");

            // Assert
            leftState.Should().Be(ForkState.InUse);  // Вилка 1 занята Платоном
            rightState.Should().Be(ForkState.Available); // Вилка 5 свободна
        }
    }
}
