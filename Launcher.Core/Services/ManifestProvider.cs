using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core.Services
{
    public class ManifestProvider : IManifestProvider
    {
        private readonly HttpClient _httpClient;

        public ManifestProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Result<RemoteManifest>> GetAsync(Uri manifestUri, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync(manifestUri, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var manifest = JsonSerializer.Deserialize<RemoteManifest>(json);

                if (manifest == null)
                    return Result<RemoteManifest>.Fail("Manifest deserialization returned null");

                return Result<RemoteManifest>.Ok(manifest);
            }
            catch (Exception ex)
            {
                return Result<RemoteManifest>.Fail($"Error getting manifest: {ex.Message}");
            }
        }
    }
}
