// MainWindow.xaml.cs
using System;
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


        private void MenuSalir_Click(object sender, RoutedEventArgs e) => Close();

        private void MenuActualizar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de actualización aún no implementada.", "Actualizar");
        }

        private void MenuAcercaDe_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Launcher v0.1", "Acerca de");

        private void BtnActualizar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnRevisarActualizaciones_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnReiniciar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnAbrirNoticia_Click(object sender, RoutedEventArgs e) { /* TODO */ }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* opcional */ }

        // Handler agregado para probar el botón "Jugar"
        private void BtnJugar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Has pulsado Jugar.", "Jugar");
        }
    }
}
