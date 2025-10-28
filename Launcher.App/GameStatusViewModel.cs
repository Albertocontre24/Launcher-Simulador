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

        // 📰 Datos del título y fecha de las noticias
        private string _tituloNoticiaActual = "Cargando...";
        private string _tituloNoticiaAnterior = "Cargando...";
        private string _fechaNoticiaActual = "";
        private string _fechaNoticiaAnterior = "";

        // 📰 Variables internas para guardar las dos noticias
        private string _ultimaNoticiaActual = "Cargando...";
        private string _ultimaNoticiaAnterior = "Cargando...";
        private string _ultimaNoticia = "Cargando noticias...";

        // 📰 Propiedad visible en la interfaz (nombre + fecha del release)
        private string _tituloNoticia = "Cargando...";
        public string TituloNoticia
        {
            get => _tituloNoticia;
            set
            {
                if (_tituloNoticia != value)
                {
                    _tituloNoticia = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string UltimaNoticia
        {
            get => _ultimaNoticia;
            set
            {
                if (_ultimaNoticia != value)
                {
                    _ultimaNoticia = value;
                    OnPropertyChanged();
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
            _ = CargarNoticiasAsync(); // 🆕 carga las noticias al iniciar
        }

        // 📰 Obtener notas del último release de GitHub
        public async Task CargarNoticiasAsync()
        {
            try
            {
                string url = "https://api.github.com/repos/Albertocontre24/Launcher-Simulador/releases";
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherClient/1.0");

                Console.WriteLine($"🌐 Consultando noticias desde: {url}");
                var response = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var releases = doc.RootElement.EnumerateArray().ToList();

                if (releases.Count == 0)
                {
                    UltimaNoticia = "No se encontraron releases en el repositorio.";
                    return;
                }

                // 🟢 Último release
                var latest = releases[0];
                _tituloNoticiaActual = latest.GetProperty("name").GetString() ?? "Sin nombre";
                _fechaNoticiaActual = DateTime.Parse(latest.GetProperty("published_at").GetString() ?? DateTime.Now.ToString()).ToString("dd/MM/yyyy");
                _ultimaNoticiaActual = latest.GetProperty("body").GetString()?.Trim() ?? "Sin descripción del último release.";

                // 🟡 Release anterior (si existe)
                if (releases.Count > 1)
                {
                    var prev = releases[1];
                    _tituloNoticiaAnterior = prev.GetProperty("name").GetString() ?? "Sin nombre";
                    _fechaNoticiaAnterior = DateTime.Parse(prev.GetProperty("published_at").GetString() ?? DateTime.Now.ToString()).ToString("dd/MM/yyyy");
                    _ultimaNoticiaAnterior = prev.GetProperty("body").GetString()?.Trim() ?? "Sin descripción del release anterior.";
                }
                else
                {
                    _tituloNoticiaAnterior = "Sin release anterior";
                    _fechaNoticiaAnterior = "";
                    _ultimaNoticiaAnterior = "No hay información de una versión anterior.";
                }

                // Mostrar por defecto el actual
                TituloNoticia = $"{_tituloNoticiaActual} — {_fechaNoticiaActual}";
                UltimaNoticia = _ultimaNoticiaActual;

                Console.WriteLine("📰 Noticias cargadas correctamente (actual + anterior).");
            }
            catch (Exception ex)
            {
                UltimaNoticia = $"Error al cargar noticias: {ex.Message}";
                Console.WriteLine($"💥 Error al cargar noticias: {ex.Message}");
            }
        }

        // 🔄 Cambiar la noticia mostrada (entre actual y anterior)
        public void CambiarNoticia(bool mostrarAnterior)
        {
            if (mostrarAnterior)
            {
                UltimaNoticia = _ultimaNoticiaAnterior;
                TituloNoticia = $"{_tituloNoticiaAnterior} — {_fechaNoticiaAnterior}";
            }
            else
            {
                UltimaNoticia = _ultimaNoticiaActual;
                TituloNoticia = $"{_tituloNoticiaActual} — {_fechaNoticiaActual}";
            }
        }

        // 📦 Cargar manifest remoto (GitHub)
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

        // 🧱 Cargar manifest local
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

                _configService.Config.VersionInstalada = UltimaVersion;
                _configService.Save();

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
