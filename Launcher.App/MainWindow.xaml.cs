// MainWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Launcher.Core.Services;
using Microsoft.Extensions.Logging;

namespace Launcher.App;

public partial class MainWindow : Window
{
	private readonly IRemoteManifestService _manifestService;
	private readonly ILogger<MainWindow> _logger;

	public MainWindow(IRemoteManifestService manifestService, ILogger<MainWindow> logger)
	{
		InitializeComponent();
		_manifestService = manifestService;
		_logger = logger;
	}

	private void MenuSalir_Click(object sender, RoutedEventArgs e) => Close();
	private void MenuActualizar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void MenuAcercaDe_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Launcher v0.1", "Acerca de");

        private void BtnJugar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
        private void BtnActualizar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnConfiguracion_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	
	// Handler para revisar actualizaciones: realiza petición al manifest (local por ahora)
	private async void BtnRevisarActualizaciones_Click(object sender, RoutedEventArgs e)
	{
		// URL local temporal; ajustar a configuración/entorno cuando haya CDN
		const string manifestUrl = "http://localhost:8000/manifest.json";
		_logger.LogInformation($"[MainWindow] Requesting manifest from {manifestUrl}");

		var result = await _manifestService.GetRemoteManifestAsync(manifestUrl).ConfigureAwait(true);

		if (result.Status == FetchStatus.Success && result.Data != null)
		{
			var version = result.Data.Version ?? "(sin versión)";
			_logger.LogInformation($"[MainWindow] Manifest fetched successfully, version={version}");
			MessageBox.Show($"Manifest obtenido. Versión: {version}", "Actualizaciones");
		}
		else
		{
			var msg = result.ErrorMessage ?? "Error desconocido";
			_logger.LogError($"[MainWindow] Error fetching manifest: {msg}");
			MessageBox.Show($"No se pudo obtener el manifest:\n{msg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	
	private void BtnReiniciar_Click(object sender, RoutedEventArgs e) { /* TODO */ }
	private void BtnAbrirNoticia_Click(object sender, RoutedEventArgs e) { /* TODO */ }

	private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* opcional */ }

	// Handler agregado para probar el botón "Jugar"
	private void BtnJugar_Click(object sender, RoutedEventArgs e)
	{
		MessageBox.Show("Has pulsado Jugar.", "Jugar");
	}
}