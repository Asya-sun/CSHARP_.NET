using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Interfaces;
using Philosophers.Core.Models;
using Philosophers.Core.Models.Enums;
using Philosophers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Philosophers.Tests
{

    /*
     * Нужен, чтобы отключить задержки, дать доступ к состоянию,
     * дать возможность запускать одну итерацию цикла
     * 
     * 
     */
    public class TestPhilosopher : PhilosopherHostedService
    {
        public TestPhilosopher(
            PhilosopherName name,
            ITableManager tableManager,
            IPhilosopherStrategy strategy,
            IMetricsCollector metricsCollector,
            IOptions<SimulationOptions> options,
            ILogger<TestPhilosopher> logger)
            : base(name, tableManager, strategy, metricsCollector, options, logger)
        {
        }

        public PhilosopherName ExposedName => _name;
        public PhilosopherState ExposedState => _state;

        protected override async Task Think(CancellationToken token)
        {
            _stepsLeft = 0;
            await Task.CompletedTask;
        }

        protected override async Task<bool> TryEat(CancellationToken token)
        {
            return await _strategy.TryAcquireForksAsync(_name, _tableManager, token);
        }

        protected override async Task Eat(CancellationToken token)
        {
            _stepsLeft = 0;
            await Task.CompletedTask;
        }

        public async Task RunOneIteration(CancellationToken token)
        {
            // Выполняем одно состояние цикла
            switch (_state)
            {
                case PhilosopherState.Thinking:
                    await Think(token);
                    _state = PhilosopherState.Hungry;
                    break;

                case PhilosopherState.Hungry:
                    if (await TryEat(token))
                        _state = PhilosopherState.Eating;
                    break;

                case PhilosopherState.Eating:
                    await Eat(token);
                    _strategy.ReleaseForks(_name, _tableManager);
                    _state = PhilosopherState.Thinking;
                    break;
            }
        }
    }

}
