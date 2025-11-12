using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Services;
using Launcher.Core.Utils; // Comparador SemVer

namespace Launcher.App
{
    public class GameStatusViewModel : INotifyPropertyChanged
    {
        // Carpeta final: <base>\update\Build
        private static readonly string InstallDir =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "update", "Build"));

        // NOMBRE EXACTO del EXE del juego (fallback si no hay selección)
        private const string ExeName = "TestLauncher.exe";

        private readonly LocalConfigService _configService;
        private readonly HttpClient _http = new HttpClient();

        // Estado instalación / descarga
        private string _mensaje = "Cargando...";
        private double _progreso = 0;
        private bool _isTrabajando = false;
        private bool _instalado = false;

        // Versionado
        private string _versionInstalada = "Cargando...";
        private string _ultimaVersion = "Cargando...";
        private string _lastError = string.Empty;

        // Noticias (actual y anterior)
        private string _tituloNoticiaActual = "Cargando...";
        private string _tituloNoticiaAnterior = "Cargando...";
        private string _fechaNoticiaActual = "";
        private string _fechaNoticiaAnterior = "";
        private string _ultimaNoticiaActual = "Cargando...";
        private string _ultimaNoticiaAnterior = "Cargando...";
        private string _ultimaNoticia = "Cargando noticias...";
        private string _tituloNoticia = "Cargando...";

        // =========================
        //  Selector de Launcher
        // =========================
        public sealed record LauncherOption(string Key, string Display, string ExeName, string? Subfolder = null);

        private LauncherOption _selectedLauncher;

        // Ajusta esta lista a tus ejecutables reales. Puedes añadir o quitar opciones sin tocar más lógica.
        public IReadOnlyList<LauncherOption> Launchers { get; } =
            new List<LauncherOption>
            {
                new LauncherOption("main",   "Launcher principal",           "TestLauncher.exe"),
                new LauncherOption("altdx11","Launcher DX11 (alternativo)", "TestLauncherDX11.exe", "DX11"),
                // Ejemplo: nuevo ejecutable en subcarpeta "Legacy"
                // new LauncherOption("legacy","Launcher Legacy", "TestLauncherLegacy.exe", "Legacy")
            };

        public LauncherOption SelectedLauncher
        {
            get => _selectedLauncher;
            set
            {
                if (_selectedLauncher != value)
                {
                    _selectedLauncher = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RutaExe));
                    OnPropertyChanged(nameof(NombreExe));
                    OnPropertyChanged(nameof(PuedeIniciar));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string TituloNoticia
        {
            get => _tituloNoticia;
            set { if (_tituloNoticia != value) { _tituloNoticia = value; OnPropertyChanged(); } }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set { if (_mensaje != value) { _mensaje = value; OnPropertyChanged(); } }
        }

        public double Progreso
        {
            get => _progreso;
            set
            {
                var v = Math.Max(0, Math.Min(100, value));
                if (Math.Abs(_progreso - v) > 0.001)
                {
                    _progreso = v;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTrabajando
        {
            get => _isTrabajando;
            private set { if (_isTrabajando != value) { _isTrabajando = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeIniciar)); } }
        }

        public bool Instalado
        {
            get => _instalado;
            private set { if (_instalado != value) { _instalado = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeIniciar)); } }
        }

        // Ahora depende de que exista el EXE de la selección actual
        public bool PuedeIniciar => File.Exists(RutaExe) && !IsTrabajando;

        public string RutaExe
        {
            get
            {
                var exe = SelectedLauncher?.ExeName ?? ExeName;
                var sub = SelectedLauncher?.Subfolder;
                return string.IsNullOrWhiteSpace(sub)
                    ? Path.Combine(InstallDir, exe)
                    : Path.Combine(InstallDir, sub!, exe);
            }
        }

        /// <summary>
        /// Solo el nombre del ejecutable (p.ej. "TestLauncher.exe") para mostrar en UI.
        /// </summary>
        public string NombreExe
        {
            get
            {
                try { return Path.GetFileName(RutaExe) ?? string.Empty; }
                catch { return string.Empty; }
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
            set { if (_ultimaNoticia != value) { _ultimaNoticia = value; OnPropertyChanged(); } }
        }

        public string LastError
        {
            get => _lastError;
            set { if (_lastError != value) { _lastError = value; OnPropertyChanged(); } }
        }

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

        // Constructor
        public GameStatusViewModel()
        {
            Console.WriteLine("🚀 Inicializando GameStatusViewModel...");

            _configService = new LocalConfigService();
            VersionInstalada = _configService.Config.VersionInstalada ?? "Desconocida";

            // Selección por defecto (primera opción)
            _selectedLauncher = Launchers[0];

            // Cabeceras para GitHub (manifest + assets)
            _http.DefaultRequestHeaders.UserAgent.Clear();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherClient/1.0");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/octet-stream");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            _ = CargarNoticiasAsync();
        }

        // Arranque: comprobar / descargar si no está / actualizar
        public async Task InicializarAsync(CancellationToken ct = default)
        {
            try
            {
                IsTrabajando = true;
                Mensaje = "Comprobando instalación...";
                Progreso = 0;

                Directory.CreateDirectory(InstallDir);

                // Instalado si existe el EXE de la opción actual
                Instalado = File.Exists(RutaExe);

                var manifest = await GetManifestPreferRemoteAsync(ct); // actualiza UltimaVersion

                if (!Instalado)
                {
                    Mensaje = "No instalado. Descargando...";
                    await DescargarEInstalarAsync(manifest, ct);

                    // Tras instalar, considera instalado si existe CUALQUIER EXE de la lista
                    Instalado = AnyLauncherExeExists();
                    if (!Instalado) Mensaje = "La instalación no se completó.";
                }
                else
                {
                    Mensaje = "Instalación detectada. Comprobando actualizaciones...";
                    if (manifest != null &&
                        !string.IsNullOrWhiteSpace(manifest.Version) &&
                        SemVerComparer.IsRemoteNewer(VersionInstalada, manifest.Version))
                    {
                        await DescargarEInstalarAsync(manifest, ct);

                        Instalado = AnyLauncherExeExists();
                        if (!Instalado) Mensaje = "Actualización incompleta.";
                    }
                    else
                    {
                        Mensaje = "Listo. Instalación al día.";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Mensaje = "Operación cancelada.";
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                LastError = ex.Message;
            }
            finally
            {
                Progreso = 0;
                IsTrabajando = false;
            }
        }

        // Noticias
        public async Task CargarNoticiasAsync()
        {
            try
            {
                string url = "https://api.github.com/repos/Albertocontre24/Launcher-Simulador/releases";
                Console.WriteLine($"🌐 Consultando noticias desde: {url}");
                var response = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var releases = doc.RootElement.EnumerateArray().ToList();

                if (releases.Count == 0)
                {
                    UltimaNoticia = "No se encontraron releases en el repositorio.";
                    return;
                }

                var latest = releases[0];
                _tituloNoticiaActual = latest.GetProperty("name").GetString() ?? "Sin nombre";
                _fechaNoticiaActual = DateTime.Parse(latest.GetProperty("published_at").GetString() ?? DateTime.Now.ToString()).ToString("dd/MM/yyyy");
                _ultimaNoticiaActual = latest.GetProperty("body").GetString()?.Trim() ?? "Sin descripción del último release.";

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

        // Manifest remoto con fallback a local
        private async Task<ManifestInfo?> GetManifestPreferRemoteAsync(CancellationToken ct)
        {
            try
            {
                string githubUrl = "https://raw.githubusercontent.com/Albertocontre24/Launcher-Simulador/main/manifest.json";
                Console.WriteLine($"🌐 Descargando manifest desde GitHub: {githubUrl}");
                using var response = await _http.GetAsync(githubUrl, HttpCompletionOption.ResponseHeadersRead, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);
                    if (manifest != null && !string.IsNullOrEmpty(manifest.Version))
                    {
                        UltimaVersion = manifest.Version;
                        LastError = string.Empty;
                        Console.WriteLine($"✅ Manifest remoto cargado: {UltimaVersion}");
                        return manifest;
                    }
                }

                Console.WriteLine("⚠️ No se pudo leer el manifest remoto, usando el local...");
                return await GetManifestLocalAsync(ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al acceder a GitHub: {ex.Message}");
                return await GetManifestLocalAsync(ct);
            }
        }

        private async Task<ManifestInfo?> GetManifestLocalAsync(CancellationToken ct)
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
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath, ct);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);

                UltimaVersion = manifest?.Version ?? "Desconocida";
                LastError = string.Empty;
                Console.WriteLine($"📂 Manifest local leído correctamente: {UltimaVersion}");
                return manifest;
            }
            catch (Exception ex)
            {
                UltimaVersion = "Error";
                LastError = ex.Message;
                Console.WriteLine($"💥 Error leyendo manifest local: {ex.Message}");
                return null;
            }
        }

        // Descargar y EXTRAER a %LOCALAPPDATA% y luego COPIAR a <base>\update\Build (anti AV/CFA)
        private async Task DescargarEInstalarAsync(ManifestInfo? manifest, CancellationToken ct)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageUrl))
            {
                Mensaje = "No hay paquete para descargar (manifest vacío).";
                Console.WriteLine("⚠️ No se especificó la URL del paquete en el manifest.");
                return;
            }

            string? tempZip = null;
            string? workRoot = null;

            try
            {
                IsTrabajando = true;

                var updateRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "update"));
                var buildRoot = InstallDir;
                Directory.CreateDirectory(updateRoot);

                // 1) Descarga ZIP a %LOCALAPPDATA%
                var dlBase = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DavanteLauncher", "downloads");
                Directory.CreateDirectory(dlBase);

                tempZip = Path.Combine(dlBase, $"update_{Guid.NewGuid():N}.zip");
                Console.WriteLine($"⬇️ Descargando actualización desde: {manifest.PackageUrl}");

                using (var response = await _http.GetAsync(manifest.PackageUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(ct);
                        Mensaje = $"Error al descargar ZIP: {(int)response.StatusCode} {response.ReasonPhrase}";
                        Console.WriteLine($"💥 GET {manifest.PackageUrl} -> {(int)response.StatusCode} {response.ReasonPhrase}\n{body}");
                        return;
                    }

                    var total = response.Content.Headers.ContentLength ?? -1L;
                    var canReport = total > 0;

                    await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
                    await using var fileStream = File.Create(tempZip);

                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;

                    while ((read = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                        totalRead += read;

                        if (canReport)
                        {
                            Progreso = Math.Round((double)totalRead / total * 100, 1);
                            Mensaje = $"Descargando... {Progreso}%";
                        }
                        else
                        {
                            Mensaje = "Descargando...";
                        }
                    }
                }

                // 2) Extrae el ZIP en %LOCALAPPDATA% (fuera de Documents)
                Mensaje = "Descomprimiendo...";
                workRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DavanteLauncher", "staging", $"work_{Guid.NewGuid():N}");
                Directory.CreateDirectory(workRoot);

                // Extraemos todo el ZIP al directorio de trabajo
                ZipFile.ExtractToDirectory(tempZip, workRoot, overwriteFiles: true);

                // 3) Detecta carpeta origen a copiar (si el ZIP trae Build/ úsala; si no, copia todo)
                var srcBuild = Path.Combine(workRoot, "Build");
                var sourceDir = Directory.Exists(srcBuild) ? srcBuild : workRoot;

                // 4) Copia a <base>\update\Build con reintentos por archivo (evita locks)
                Mensaje = "Instalando...";
                await CopyDirectoryWithRetriesAsync(sourceDir, buildRoot, ct);

                // 5) Persistir versión instalada
                if (!string.IsNullOrWhiteSpace(UltimaVersion))
                {
                    _configService.Config.VersionInstalada = UltimaVersion;
                    _configService.Save();
                    VersionInstalada = UltimaVersion;
                }

                // 6) Validar ejecutables
                //    - Comprobamos TODOS los exes posibles del selector
                //    - Si no aparecen directamente, buscamos recursivamente (por si hay subcarpetas adicionales)
                bool anyExe = AnyLauncherExeExists();
                Instalado = anyExe;

                Mensaje = Instalado
                    ? "Instalación/Actualización completada correctamente."
                    : "No se encontró ningún ejecutable tras la instalación.";

                Console.WriteLine($"▶ Instalación: {(Instalado ? "OK" : "FALLÓ")} (selector activo)");
            }
            catch (OperationCanceledException)
            {
                Mensaje = "Descarga cancelada.";
                throw;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Mensaje = $"Error durante la instalación: {ex.Message}";
                Console.WriteLine($"💥 Error durante la instalación: {ex.Message}");
            }
            finally
            {
                Progreso = 0;
                IsTrabajando = false;

                // Limpieza
                try { if (tempZip is not null && File.Exists(tempZip)) File.Delete(tempZip); } catch { }
                try
                {
                    if (workRoot is not null && Directory.Exists(workRoot))
                        Directory.Delete(workRoot, recursive: true);
                }
                catch { }
            }
        }

        private static async Task CopyDirectoryWithRetriesAsync(string src, string dst, CancellationToken ct, int maxRetries = 5)
        {
            // Crea destino
            Directory.CreateDirectory(dst);

            // Copia archivos del nivel actual
            foreach (var file in Directory.EnumerateFiles(src))
            {
                ct.ThrowIfCancellationRequested();
                var destFile = Path.Combine(dst, Path.GetFileName(file));
                await CopyFileWithRetriesAsync(file, destFile, ct, maxRetries);
            }

            // Recurse subdirectorios
            foreach (var dir in Directory.EnumerateDirectories(src))
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(dir);
                var destDir = Path.Combine(dst, name);
                await CopyDirectoryWithRetriesAsync(dir, destDir, ct, maxRetries);
            }
        }

        private static async Task CopyFileWithRetriesAsync(string srcFile, string dstFile, CancellationToken ct, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
                    File.Copy(srcFile, dstFile, overwrite: true);
                    return;
                }
                catch (IOException)
                {
                    await Task.Delay(300 + i * 200, ct); // backoff por archivo en uso
                }
                catch (UnauthorizedAccessException)
                {
                    await Task.Delay(500 + i * 200, ct); // AV/CFA
                }
            }

            // último intento fuera del bucle para propagar error real si persiste
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
            File.Copy(srcFile, dstFile, overwrite: true);
        }

        // ¿Existe el EXE de la opción actual?
        private bool SelectedLauncherExeExists()
        {
            var exePath = RutaExe;
            if (File.Exists(exePath)) return true;

            // Búsqueda recursiva de seguridad si la estructura no es la esperada
            try
            {
                var name = Path.GetFileName(exePath);
                var found = Directory.EnumerateFiles(InstallDir, name, SearchOption.AllDirectories).Any();
                return found;
            }
            catch { return false; }
        }

        // ¿Existe CUALQUIERA de los EXEs configurados?
        private bool AnyLauncherExeExists()
        {
            foreach (var opt in Launchers)
            {
                var basePath = string.IsNullOrWhiteSpace(opt.Subfolder)
                    ? InstallDir
                    : Path.Combine(InstallDir, opt.Subfolder!);

                var direct = Path.Combine(basePath, opt.ExeName);
                if (File.Exists(direct)) return true;

                try
                {
                    // fallback recursivo por si el ZIP trae subcarpetas adicionales
                    if (Directory.Exists(basePath) &&
                        Directory.EnumerateFiles(basePath, opt.ExeName, SearchOption.AllDirectories).Any())
                        return true;
                }
                catch { /* ignore */ }
            }
            return false;
        }

        private class ManifestInfo
        {
            public string? Version { get; set; }
            public string? PackageUrl { get; set; }
        }
    }
}
