// MainWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace Launcher.App;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private void MenuSalir_Click(object sender, RoutedEventArgs e) => Close();
	private void MenuActualizar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void MenuAcercaDe_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Launcher v0.1", "Acerca de");

        private void BtnJugar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnActualizar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnConfiguracion_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnRevisarActualizaciones_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnReiniciar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnAbrirNoticia_Click(object sender, RoutedEventArgs e) { /* TODO */ }

	private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* opcional */ }
}