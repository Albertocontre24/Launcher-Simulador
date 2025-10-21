using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Services;
using Launcher.Core.Utils; // ✅ Importamos el comparador SemVer

namespace Launcher.App
{
    public class GameStatusViewModel : INotifyPropertyChanged
    {
        private readonly LocalConfigService _configService;

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

        // ✅ Usa comparador SemVer para determinar estado
        public string IconoEstado
        {
            get
            {
                if (UltimaVersion == "Cargando..." || UltimaVersion == "Error")
                    return "Assets/loading.png";

                try
                {
                    return SemVerComparer.IsRemoteNewer(VersionInstalada, UltimaVersion)
                        ? "Assets/warning.png"   // Hay actualización
                        : "Assets/tick.png";      // Todo actualizado
                }
                catch
                {
                    return "Assets/error.png";
                }
            }
        }

        public string TextoEstado
        {
            get
            {
                if (UltimaVersion == "Cargando...")
                    return " Comprobando versiones...";
                if (UltimaVersion == "Error")
                    return $" Error: {LastError}";

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

            _ = CargarManifestLocalAsync();
        }

        // 📦 Cargar manifest local (o remoto si lo prefieres después)
        public async Task CargarManifestLocalAsync()
        {
            try
            {
                Console.WriteLine("🔍 Iniciando carga del manifest local...");

                var provider = new LocalManifestProvider();
                var manifestPath = Path.Combine(AppContext.BaseDirectory, "manifest.json");

                Console.WriteLine($"📁 Ruta esperada: {manifestPath}");

                if (!File.Exists(manifestPath))
                {
                    Console.WriteLine("❌ No se encontró el archivo manifest.json");
                    UltimaVersion = "Error";
                    LastError = "Archivo manifest.json no encontrado.";
                    return;
                }

                var uri = new Uri(manifestPath);
                var result = await provider.GetAsync(uri, CancellationToken.None);

                if (result.IsSuccess && result.Value != null)
                {
                    UltimaVersion = result.Value.Version ?? "Desconocida";
                    LastError = string.Empty;
                    Console.WriteLine($"✅ Manifest leído correctamente. Versión: {UltimaVersion}");
                }
                else
                {
                    UltimaVersion = "Error";
                    LastError = result.Error ?? "No se pudo leer el manifest.";
                    Console.WriteLine($"⚠️ Error al leer manifest: {LastError}");
                }
            }
            catch (Exception ex)
            {
                UltimaVersion = "Error";
                LastError = $"Excepción: {ex.Message}";
                Console.WriteLine($"💥 Excepción al leer manifest: {ex}");
            }
        }
    }
}
