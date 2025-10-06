using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Launcher.Core.Models;
using Microsoft.Extensions.Logging;

namespace Launcher.Core.Services;

public class RemoteManifestService : IRemoteManifestService
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;
    private readonly ILogger<RemoteManifestService> _logger;

    public RemoteManifestService(HttpClient? httpClient = null, TimeSpan? timeout = null, ILogger<RemoteManifestService>? logger = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(15);
        _httpClient = httpClient ?? new HttpClient { Timeout = _timeout };
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    private void LogError(string message)
    {
        _logger.LogError(message);
    }

    public async Task<FetchResult<RemoteManifest>> GetRemoteManifestAsync(string manifestUrl)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return FetchResult<RemoteManifest>.FailureResult("Manifest URL is null or empty");

        LogInformation($"[RemoteManifestService] Starting request to {manifestUrl}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var msg = $"HTTP request failed with status code {response.StatusCode}";
                LogError($"[RemoteManifestService] {msg}");
                return FetchResult<RemoteManifest>.FailureResult(msg);
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                var msg = "Empty response content";
                LogError($"[RemoteManifestService] {msg}");
                return FetchResult<RemoteManifest>.FailureResult(msg);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            RemoteManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<RemoteManifest>(content, options);
            }
            catch (JsonException je)
            {
                var msg = $"JSON deserialization error: {je.Message}";
                LogError($"[RemoteManifestService] {msg}");
                return FetchResult<RemoteManifest>.FailureResult(msg);
            }

            if (manifest == null)
            {
                var msg = "Deserialized manifest is null";
                LogError($"[RemoteManifestService] {msg}");
                return FetchResult<RemoteManifest>.FailureResult(msg);
            }

            LogInformation($"[RemoteManifestService] Successfully fetched and deserialized manifest (version={manifest.Version})");
            return FetchResult<RemoteManifest>.SuccessResult(manifest);
        }
        catch (TaskCanceledException tce) when (!tce.CancellationToken.IsCancellationRequested)
        {
            // Timeout
            var msg = $"Request timed out after {_timeout.TotalSeconds} seconds";
            LogError($"[RemoteManifestService] {msg}");
            return FetchResult<RemoteManifest>.FailureResult(msg);
        }
        catch (Exception ex)
        {
            var msg = $"Network or unexpected error: {ex.Message}";
            LogError($"[RemoteManifestService] {msg}");
            return FetchResult<RemoteManifest>.FailureResult(msg);
        }
    }
}
