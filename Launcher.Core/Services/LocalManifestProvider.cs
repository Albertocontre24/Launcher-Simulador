using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core.Services
{
    public sealed class RemoteManifest
    {
        public string Version { get; set; } = "0.0.0";
        public string Channel { get; set; } = "stable";
        public string PackageUrl { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? NotesUrl { get; set; }
    }

    public interface IManifestProvider
    {
        Task<Result<RemoteManifest>> GetAsync(Uri manifestUri, CancellationToken ct);
    }

    public sealed class LocalManifestProvider : IManifestProvider
    {
        public async Task<Result<RemoteManifest>> GetAsync(Uri uri, CancellationToken ct)
        {
            try
            {
                var path = uri.LocalPath;
                Console.WriteLine($"[Manifest] Leyendo manifest local desde: {path}");

                if (!File.Exists(path))
                    return Result<RemoteManifest>.Fail($"Archivo no encontrado: {path}");

                var json = await File.ReadAllTextAsync(path, ct);
                if (string.IsNullOrWhiteSpace(json))
                    return Result<RemoteManifest>.Fail("El archivo manifest está vacío.");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var manifest = JsonSerializer.Deserialize<RemoteManifest>(json, options);
                if (manifest == null)
                    return Result<RemoteManifest>.Fail("No se pudo deserializar el manifest.");

                Console.WriteLine($"[Manifest] Cargado correctamente (versión: {manifest.Version})");
                return Result<RemoteManifest>.Ok(manifest);
            }
            catch (JsonException jex)
            {
                return Result<RemoteManifest>.Fail($"Error JSON: {jex.Message}");
            }
            catch (Exception ex)
            {
                return Result<RemoteManifest>.Fail($"Error al leer manifest: {ex.Message}");
            }
        }
    }
}
