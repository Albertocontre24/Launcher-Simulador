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
        public string PackageUrl { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long Size { get; set; }
        public string? NotesUrl { get; set; }
    }

    public sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public T? Value { get; }

        private Result(bool success, T? value, string? error)
        {
            IsSuccess = success;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);
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
                    return Result<RemoteManifest>.Failure($"Archivo no encontrado: {path}");

                var json = await File.ReadAllTextAsync(path, ct);
                if (string.IsNullOrWhiteSpace(json))
                    return Result<RemoteManifest>.Failure("El archivo manifest está vacío.");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var manifest = JsonSerializer.Deserialize<RemoteManifest>(json, options);
                if (manifest == null)
                    return Result<RemoteManifest>.Failure("No se pudo deserializar el manifest.");

                Console.WriteLine($"[Manifest] Cargado correctamente (versión: {manifest.Version})");
                return Result<RemoteManifest>.Success(manifest);
            }
            catch (JsonException jex)
            {
                return Result<RemoteManifest>.Failure($"Error JSON: {jex.Message}");
            }
            catch (Exception ex)
            {
                return Result<RemoteManifest>.Failure($"Error al leer manifest: {ex.Message}");
            }
        }
    }
}
