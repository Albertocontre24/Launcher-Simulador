using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Services;

namespace Launcher.App
{
    public class GameStatusViewModel : INotifyPropertyChanged
    {
        private string _versionInstalada = "1.2.0";
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

        public string IconoEstado =>
            VersionInstalada == UltimaVersion
                ? "Assets/tick.png"
                : "Assets/warning.png";

        public string TextoEstado =>
            VersionInstalada == UltimaVersion
                ? " El launcher está actualizado y listo para jugar"
                : " Hay una actualización disponible";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Constructor: carga el manifest automáticamente
        public GameStatusViewModel()
        {
            _ = CargarManifestLocalAsync();
        }

        // Método para cargar el manifest local
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
