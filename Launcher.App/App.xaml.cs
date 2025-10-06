using System.Configuration;
using System.Data;
using System.Windows;

namespace Launcher.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            // Tu código normal aquí
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Error en arranque");
            Environment.Exit(1);
        }
    }

}

