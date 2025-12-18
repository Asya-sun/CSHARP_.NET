using System.Net.Http.Json;
using Philosophers.Shared.DTO;

namespace PhilosopherService.Http
{
    public class TableClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TableClient> _logger;

        public TableClient(HttpClient httpClient, ILogger<TableClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RegisterAsync(
            RegisterPhilosopherRequest request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/table/philosophers/register",
                request,
                ct);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Philosopher {Name} registered",
                request.Name);
        }

        public async Task UnregisterAsync(
            UnregisterPhilosopherRequest request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/table/philosophers/unregister",
                request,
                ct);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Philosopher {Id} unregistered",
                request.PhilosopherId);
        }

        public async Task<bool> TryAcquireForkAsync(
            AcquireForkRequest request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/table/forks/{request.ForkId}/acquire",
                request,
                ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AcquireForkResponse>(ct);

            return result?.Success ?? false;
        }

        public async Task ReleaseForkAsync(
            ReleaseForkRequest request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/table/forks/{request.ForkId}/release",
                request,
                ct);

            response.EnsureSuccessStatusCode();
        }


        public async Task UpdateStateAsync(
            PhilosopherStateUpdate request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/table/philosophers/{request.PhilosopherId}/state",
                request,
                ct);

            response.EnsureSuccessStatusCode();
        }
    }
}
