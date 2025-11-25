//using FluentAssertions;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Moq;
//using Philosophers.Core.Models;
//using Philosophers.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Philosophers.Tests
//{
//    public class MetricsCollectorTests
//    {
//        private readonly Mock<ILogger<MetricsCollector>> _loggerMock;
//        private readonly Mock<IOptions<SimulationOptions>> _optionsMock;
//        private readonly MetricsCollector _metricsCollector;

//        public MetricsCollectorTests()
//        {
//            _loggerMock = new Mock<ILogger<MetricsCollector>>();
//            _optionsMock = new Mock<IOptions<SimulationOptions>>();

//            _optionsMock.Setup(o => o.Value).Returns(new SimulationOptions
//            {
//                DurationSeconds = 120
//            });

//            _metricsCollector = new MetricsCollector(_loggerMock.Object, _optionsMock.Object);
//        }



//        // проверяем, что при вызове RecordEating счетчик увеличивается
//        [Fact]
//        public void RecordEating_ShouldIncrementCounter()
//        {
//            // Arrange
//            var philosopher = "Платон";

//            // Act
//            _metricsCollector.RecordEating(philosopher);
//            _metricsCollector.RecordEating(philosopher);
//            _metricsCollector.RecordEating(philosopher);

//            // Assert
//            _metricsCollector.GetEatCount(philosopher).Should().Be(3);
//        }


//        // проверяем, что для неизвестного философа возвр 0 (защита от NullReferenceException)
//        [Fact]
//        public void GetEatCount_ShouldReturnZero_ForUnknownPhilosopher()
//        {
//            // Act
//            var result = _metricsCollector.GetEatCount("Неизвестный");

//            // Assert
//            result.Should().Be(0);
//        }


//        //+
//        [Fact]
//        public void RecordThinkingTime_ShouldStoreCorrectThinkingTimes()
//        {
//            // Arrange
//            var philosopher = "Платон";
//            var thinkingTime1 = TimeSpan.FromMilliseconds(180);
//            var thinkingTime2 = TimeSpan.FromMilliseconds(210);

//            // Act
//            _metricsCollector.RecordThinkingTime(philosopher, thinkingTime1);
//            _metricsCollector.RecordThinkingTime(philosopher, thinkingTime2);


//            var thinkingTimes = _metricsCollector.GetThinkingTimes()[philosopher];
//            thinkingTimes.Should().HaveCount(2);

//            thinkingTimes.Should().Contain(thinkingTime1);
//            thinkingTimes.Should().Contain(thinkingTime2);
//        }


//        //+
//        [Fact]
//        public void RecordEatingTime_ShouldStoreCorrectEatingTimes()
//        {
//            // Arrange
//            var philosopher = "Платон";
//            var eatingTime1 = TimeSpan.FromMilliseconds(180);
//            var eatingTime2 = TimeSpan.FromMilliseconds(210);

//            // Act
//            _metricsCollector.RecordEatingTime(philosopher, eatingTime1);
//            _metricsCollector.RecordEatingTime(philosopher, eatingTime2);


//            var eatingTimes = _metricsCollector.GetEatingTimes()[philosopher];
//            eatingTimes.Should().HaveCount(2);

//            eatingTimes.Should().Contain(eatingTime1);
//            eatingTimes.Should().Contain(eatingTime2);
//        }


//        //+
//        [Fact]
//        public void RecordWaitingTime_ShouldStoreCorrectWaitingTimes()
//        {
//            // Arrange
//            var philosopher = "Платон";
//            var waitingTime1 = TimeSpan.FromMilliseconds(150);
//            var waitingTime2 = TimeSpan.FromMilliseconds(200);

//            // Act
//            _metricsCollector.RecordWaitingTime(philosopher, waitingTime1);
//            _metricsCollector.RecordWaitingTime(philosopher, waitingTime2);


//            var waitingTimes = _metricsCollector.GetWaitingTimes()[philosopher];
//            waitingTimes.Should().HaveCount(2);

//            waitingTimes.Should().Contain(waitingTime1);
//            waitingTimes.Should().Contain(waitingTime2);
//        }

//        //+
//        [Fact]
//        public void RecordForkAcquiredAndReleased_ShouldTrackUsageTime()
//        {
//            // Arrange
//            var forkId = 1;
//            var philosopher = "Платон";

//            // Act
//            _metricsCollector.RecordForkAcquired(forkId, philosopher);
//            Thread.Sleep(50);
//            _metricsCollector.RecordForkReleased(forkId);

//            // Assert
//            var forkUsage = _metricsCollector.GetForkUsageTimes();
//            forkUsage.Should().ContainKey(forkId);
//            forkUsage[forkId].TotalMilliseconds.Should().BeGreaterThanOrEqualTo(50);
//            forkUsage[forkId].TotalMilliseconds.Should().BeLessThan(65);
//        }


//        //+
//        [Fact]
//        public void RecordForkReleased_ShouldNotChangeUsageTime_WhenForkNotAcquired()
//        {
//            // Arrange
//            var forkId = 2;
//            var expectedUsage = TimeSpan.Zero;

//            // Act
//            _metricsCollector.RecordForkReleased(forkId);

//            // Assert
//            _metricsCollector.GetForkUsageTimes()[forkId].Should().Be(expectedUsage);
//        }



//        //+
//        [Fact]
//        public void MultiplePhilosophers_ShouldTrackSeparately()
//        {
//            // Arrange
//            var plato = "Платон";
//            var aristotle = "Аристотель";

//            // Act
//            _metricsCollector.RecordEating(plato);
//            _metricsCollector.RecordEating(plato);
//            _metricsCollector.RecordEating(aristotle);

//            // Assert
//            _metricsCollector.GetEatCount(plato).Should().Be(2);
//            _metricsCollector.GetEatCount(aristotle).Should().Be(1);
//        }


//    }
//}
