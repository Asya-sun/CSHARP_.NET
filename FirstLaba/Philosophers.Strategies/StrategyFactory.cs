using Philosophers.Core.Interfaces;
using Philosophers.Strategies.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Philosophers.Strategies
{
    public class StrategyFactory : IStrategyFactory
    {
        public IPhilosopherStrategy CreateStrategy(string strategyName, ICoordinator coordinator = null)
        {
            return strategyName switch
            {
                "Naive" => new NaiveStrategy(),
                "Coordinated" => CreateCoordinatedStrategy(coordinator),
                _ => throw new ArgumentException($"Unknown strategy: {strategyName}")
            };

        }

        public IPhilosopherStrategy CreateCoordinatedStrategy(ICoordinator coordinator)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator), "Coordinator is required for coordinated strategy");
            var strategy = new CoordinatedStrategy();
            strategy.SetCoordinator(coordinator);
            return strategy;
        }


    }
}
