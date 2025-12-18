using PhilosopherService.Http;
using PhilosopherService.Models;

namespace PhilosopherService.Interfaces {
    public interface IPhilosopherStrategy
    {
        /// <summary>
        /// Метод возвращается ТОЛЬКО когда философ владеет обеими вилками
        /// </summary>
        Task AcquireForksAsync(
            PhilosopherConfig config,
            TableClient tableClient,
            CancellationToken cancellationToken);

        Task ReleaseForksAsync(
            PhilosopherConfig config,
            TableClient tableClient);
    }
}
