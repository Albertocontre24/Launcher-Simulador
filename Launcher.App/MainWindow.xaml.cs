// MainWindow.xaml.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.App
{
    public partial class MainWindow : Window
    {
        // Expuesto para que tus bindings {Binding GameStatus.*} funcionen en XAML
        public GameStatusViewModel GameStatus { get; } = new GameStatusViewModel();

        public MainWindow()
        {
            InitializeComponent();

            // El DataContext es la propia ventana, que expone .GameStatus
            DataContext = this;

            // 🔔 Suscribirse a los errores del ViewModel
            GameStatus.OnError += msg =>
            {
                // Aseguramos que se ejecute en el hilo de UI
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            };

            // Al cargar la ventana: comprobar/descargar/instalar si hace falta
            Loaded += async (_, __) =>
            {
                try
                {
                    await Task.WhenAll(
                        GameStatus.InicializarAsync(),
                        GameStatus.CargarNoticiasAsync()
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error inicializando:\n{ex.Message}", "Launcher",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        // ───── Menús ─────────────────────────────────────────────────────────────

        private void MenuSalir_Click(object sender, RoutedEventArgs e) => Close();

        private async void MenuActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GameStatus.InicializarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar:\n{ex.Message}", "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuAcercaDe_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Launcher v0.1", "Acerca de");

        // Botón “Actualizar”: reutiliza la lógica del ViewModel
        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GameStatus.InicializarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar:\n{ex.Message}", "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e) { /* opcional */ }

        private void BtnRevisarActualizaciones_Click(object sender, RoutedEventArgs e)
        {
            // Si quieres un flujo distinto a InicializarAsync, impleméntalo aquí.
            // Por defecto, con BtnActualizar_Click ya cubres la comprobación/descarga.
        }

        private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exeLauncher = Process.GetCurrentProcess().MainModule!.FileName;
                Process.Start(exeLauncher);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo reiniciar:\n{ex.Message}", "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ───── Noticias ──────────────────────────────────────────────────────────

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameStatus == null) return;
            var combo = sender as ComboBox;
            GameStatus.CambiarNoticia(mostrarAnterior: combo?.SelectedIndex == 1);
        }

        // ───── Botón Iniciar (lanza el juego) ────────────────────────────────────

        private async void BtnIniciar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exe = GameStatus.RutaExe;

                if (!File.Exists(exe))
                {
                    MessageBox.Show($"No se encontró el ejecutable:\n{exe}",
                                    "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Minimiza el launcher mientras el juego está abierto
                WindowState = WindowState.Minimized;

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
                    UseShellExecute = true
                };

                var process = Process.Start(psi);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    WindowState = WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar iniciar:\n{ex.Message}",
                                "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // (Opcional) pestañas
        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* opcional */ }

        // ───── Chips: acciones rápidas ───────────────────────────────────────────

        // Copia la ruta del ejecutable al portapapeles
        private void BtnCopiarRuta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ruta = GameStatus?.RutaExe;
                if (!string.IsNullOrWhiteSpace(ruta))
                {
                    Clipboard.SetText(ruta);
                    // Feedback opcional:
                    // MessageBox.Show("Ruta copiada al portapapeles.", "Launcher");
                }
                else
                {
                    MessageBox.Show("No hay ruta para copiar.", "Launcher",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo copiar la ruta:\n{ex.Message}", "Launcher",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Abre la carpeta que contiene el ejecutable
        private void BtnAbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ruta = GameStatus?.RutaExe;
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    MessageBox.Show("No hay ruta para abrir.", "Launcher",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dir = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("La carpeta del ejecutable no existe.", "Launcher",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la carpeta:\n{ex.Message}", "Launcher",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

