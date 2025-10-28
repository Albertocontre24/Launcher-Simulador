using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Services;
using Launcher.Core.Utils; // ✅ Comparador SemVer

namespace Launcher.App
{
    public class GameStatusViewModel : INotifyPropertyChanged
    {
        private readonly LocalConfigService _configService;
        private readonly HttpClient _httpClient = new HttpClient();

        private string _versionInstalada = "Cargando...";
        private string _ultimaVersion = "Cargando...";
        private string _lastError = string.Empty;

        public string VersionInstalada
        {
            get => _versionInstalada;
            set
            {
                if (_versionInstalada != value)
                {
                    _versionInstalada = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextoEstado));
                    OnPropertyChanged(nameof(IconoEstado));
                }
            }
        }

        public string UltimaVersion
        {
            get => _ultimaVersion;
            set
            {
                if (_ultimaVersion != value)
                {
                    _ultimaVersion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextoEstado));
                    OnPropertyChanged(nameof(IconoEstado));
                }
            }
        }

        public string LastError
        {
            get => _lastError;
            set
            {
                if (_lastError != value)
                {
                    _lastError = value;
                    OnPropertyChanged();
                }
            }
        }

        // ✅ Usa comparador SemVer
        public string IconoEstado =>
            UltimaVersion switch
            {
                "Cargando..." or "Error" => "Assets/loading.png",
                _ => SemVerComparer.IsRemoteNewer(VersionInstalada, UltimaVersion)
                    ? "Assets/warning.png"
                    : "Assets/tick.png"
            };

        public string TextoEstado
        {
            get
            {
                if (UltimaVersion == "Cargando...") return " Comprobando versiones...";
                if (UltimaVersion == "Error") return $" Error: {LastError}";

                try
                {
                    return SemVerComparer.IsRemoteNewer(VersionInstalada, UltimaVersion)
                        ? $" Hay una actualización disponible ({UltimaVersion})"
                        : $" El launcher está actualizado ({VersionInstalada})";
                }
                catch (Exception ex)
                {
                    return $" Error comparando versiones: {ex.Message}";
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // 🧠 Constructor
        public GameStatusViewModel()
        {
            Console.WriteLine("🚀 Inicializando GameStatusViewModel...");

            _configService = new LocalConfigService();
            VersionInstalada = _configService.Config.VersionInstalada ?? "Desconocida";

            _ = CargarManifestAsync();
        }

        // 📦 Cargar manifest remoto (GitHub) con respaldo local
        private async Task CargarManifestAsync()
        {
            try
            {
                string githubUrl = "https://raw.githubusercontent.com/Albertocontre24/Launcher-Simulador/main/manifest.json";

                Console.WriteLine($"🌐 Descargando manifest desde GitHub: {githubUrl}");
                var response = await _httpClient.GetAsync(githubUrl, CancellationToken.None);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);

                    if (manifest != null && !string.IsNullOrEmpty(manifest.Version))
                    {
                        UltimaVersion = manifest.Version;
                        LastError = string.Empty;
                        Console.WriteLine($"✅ Manifest remoto cargado: {UltimaVersion}");

                        // Si hay una versión nueva, descarga y actualiza
                        if (SemVerComparer.IsRemoteNewer(VersionInstalada, UltimaVersion))
                        {
                            await DescargarYActualizarAsync(manifest);
                        }
                        return;
                    }
                }

                Console.WriteLine("⚠️ No se pudo leer el manifest remoto, usando el local...");
                await CargarManifestLocalAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al acceder a GitHub: {ex.Message}");
                await CargarManifestLocalAsync();
            }
        }

        // 🧱 Cargar manifest local como respaldo
        private async Task CargarManifestLocalAsync()
        {
            try
            {
                var manifestPath = Path.Combine(AppContext.BaseDirectory, "manifest.json");
                Console.WriteLine($"📁 Intentando cargar manifest local en: {manifestPath}");

                if (!File.Exists(manifestPath))
                {
                    UltimaVersion = "Error";
                    LastError = "No se encontró manifest.json local.";
                    Console.WriteLine("❌ No se encontró manifest.json local.");
                    return;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);

                UltimaVersion = manifest?.Version ?? "Desconocida";
                LastError = string.Empty;
                Console.WriteLine($"📂 Manifest local leído correctamente: {UltimaVersion}");
            }
            catch (Exception ex)
            {
                UltimaVersion = "Error";
                LastError = ex.Message;
                Console.WriteLine($"💥 Error leyendo manifest local: {ex.Message}");
            }
        }

        // 🧩 Método de actualización completo
        private async Task DescargarYActualizarAsync(ManifestInfo manifest)
        {
            try
            {
                if (string.IsNullOrEmpty(manifest.PackageUrl))
                {
                    Console.WriteLine("⚠️ No se especificó la URL del paquete en el manifest.");
                    return;
                }

                string tempZip = Path.Combine(Path.GetTempPath(), "update.zip");
                Console.WriteLine($"⬇️ Descargando actualización desde: {manifest.PackageUrl}");

                var data = await _httpClient.GetByteArrayAsync(manifest.PackageUrl);
                await File.WriteAllBytesAsync(tempZip, data);

                string extractPath = AppContext.BaseDirectory;
                Console.WriteLine($"📦 Descomprimiendo en: {extractPath}");

                ZipFile.ExtractToDirectory(tempZip, extractPath, true);

                File.Delete(tempZip);
                Console.WriteLine("✅ Actualización completada correctamente.");

                // 🟢 Actualizar config local
                _configService.Config.VersionInstalada = UltimaVersion;
                _configService.Save();

                // 🟢 Refrescar UI
                VersionInstalada = UltimaVersion;
                OnPropertyChanged(nameof(VersionInstalada));
                OnPropertyChanged(nameof(TextoEstado));
                OnPropertyChanged(nameof(IconoEstado));

                Console.WriteLine($"✅ Versión actualizada a {UltimaVersion}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error durante la actualización: {ex.Message}");
                LastError = ex.Message;
            }
        }

        private class ManifestInfo
        {
            public string? Version { get; set; }
            public string? PackageUrl { get; set; }
        }
    }
}
