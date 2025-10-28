using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Launcher.Core.Services
{
    public class GitHubNewsService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetLatestReleaseNotesAsync()
        {
            // 🔗 URL de tu repositorio (cambia por el tuyo)
            string url = "https://api.github.com/repos/Albertocontre24/Launcher-Simulador/releases/latest";

            // GitHub requiere un User-Agent
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherClient/1.0");

            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("body", out var body))
                return body.GetString()?.Trim() ?? "Sin descripción.";

            return "Sin noticias recientes.";
        }
    }
}
