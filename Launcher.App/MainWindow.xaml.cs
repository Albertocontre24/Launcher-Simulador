// MainWindow.xaml.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Launcher.Core.Services;

namespace Launcher.App
{
    public partial class MainWindow : Window
    {
        public GameStatusViewModel GameStatus { get; set; }

        // Servicio de configuración local
        private readonly LocalConfigService _configService;
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();

            // 1️⃣ Crear instancia del servicio local primero
            _configService = new LocalConfigService();

            // 2️⃣ Inicializar ViewModel después
            GameStatus = new GameStatusViewModel();
            DataContext = this;

            // 3️⃣ Mostrar información en consola (solo para debug)
            Console.WriteLine($"📁 Archivo config: {_configService.GetConfigPath()}");
            Console.WriteLine($"⚙️  Versión instalada: {_configService.Config.VersionInstalada}");

            // 4️⃣ (Opcional) modificar algún valor y guardar
            _configService.Config.MostrarLog = true;
            _configService.Save();
        }

        // 🧩 Menús
        private void MenuSalir_Click(object sender, RoutedEventArgs e) => Close();

        private void MenuActualizar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de actualización aún no implementada.", "Actualizar");
        }

        private void MenuAcercaDe_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Launcher v0.1", "Acerca de");

        // 🧩 BOTÓN ACTUALIZAR — Descarga y descomprime actualización
        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Comprobando actualizaciones...", "Launcher", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ URL RAW del manifest remoto (cuando el repo sea público)
                string manifestUrl = "https://raw.githubusercontent.com/Albertocontre24/Launcher-Simulador/main/manifest.json";

                // 🔹 Descargar manifest desde GitHub
                var response = await _httpClient.GetAsync(manifestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("No se pudo acceder al manifest remoto.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 🔹 Leer JSON del manifest
                var json = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);

                if (manifest == null || string.IsNullOrEmpty(manifest.PackageUrl))
                {
                    MessageBox.Show("El manifest remoto no es válido o falta el campo 'packageUrl'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 📦 Descargar ZIP de la actualización
                string zipPath = Path.Combine(AppContext.BaseDirectory, "update.zip");
                MessageBox.Show($"Descargando actualización desde:\n{manifest.PackageUrl}", "Descargando...", MessageBoxButton.OK, MessageBoxImage.Information);

                using (var stream = await _httpClient.GetStreamAsync(manifest.PackageUrl))
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // 📂 Descomprimir el ZIP descargado
                string extractPath = Path.Combine(AppContext.BaseDirectory, "update");
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                ZipFile.ExtractToDirectory(zipPath, extractPath);

                MessageBox.Show("Actualización descargada y descomprimida correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // 💡 (Opcional): Ejecutar instalador o reiniciar launcher
                // System.Diagnostics.Process.Start(Path.Combine(extractPath, "setup.exe"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Clase auxiliar para leer manifest remoto
        private class ManifestInfo
        {
            public string? Version { get; set; }
            public string? PackageUrl { get; set; }
        }

        // Otros botones (vacíos por ahora)
        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnRevisarActualizaciones_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnReiniciar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnAbrirNoticia_Click(object sender, RoutedEventArgs e) { /* TODO */ }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* opcional */ }

        private void BtnJugar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Has pulsado Jugar.", "Jugar");
        }
    }
}
