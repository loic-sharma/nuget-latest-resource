using BaGet.Protocol.Models;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BaGet.Protocol
{
    public class LatestClient
    {
        private static readonly string[] LatestResourceType = { "Latest/1.0.0" };

        private readonly IServiceIndexClient _serviceIndexClient;
        private readonly HttpClient _http;

        private string _latestUrl = null;

        public LatestClient(IServiceIndexClient serviceIndexClient, HttpClient http)
        {
            _serviceIndexClient = serviceIndexClient;
            _http = http;
        }

        public async Task<LatestResponse> GetLatestOrNullAsync(
            string packageId,
            CancellationToken cancellationToken = default)
        {
            await EnsureLatestUrlAsync(cancellationToken);

            var url = $"{_latestUrl}/{packageId.ToLowerInvariant()}/latest.json";

            return await GetFromJsonOrDefaultAsync<LatestResponse>(url, cancellationToken);
        }

        public async Task<LatestResponse> GetLatestStableOrNullAsync(
            string packageId,
            CancellationToken cancellationToken = default)
        {
            await EnsureLatestUrlAsync(cancellationToken);

            var url = $"{_latestUrl}/{packageId.ToLowerInvariant()}/latest-stable.json";

            return await GetFromJsonOrDefaultAsync<LatestResponse>(url, cancellationToken);
        }

        public async Task<LatestResponse> GetLatestPreleaseOrNullAsync(
            string packageId,
            CancellationToken cancellationToken = default)
        {
            await EnsureLatestUrlAsync(cancellationToken);

            var url = $"{_latestUrl}/{packageId.ToLowerInvariant()}/latest-prerelease.json";

            return await GetFromJsonOrDefaultAsync<LatestResponse>(url, cancellationToken);
        }

        private async Task EnsureLatestUrlAsync(CancellationToken cancellationToken)
        {
            if (_latestUrl == null)
            {
                var index = await _serviceIndexClient.GetAsync(cancellationToken);
                _latestUrl = index.GetResourceUrl(LatestResourceType);
            }
        }

        public async Task<TResult> GetFromJsonOrDefaultAsync<TResult>(
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            using (var response = await _http.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }

                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return await JsonSerializer.DeserializeAsync<TResult>(stream);
                }
            }
        }
    }
}
