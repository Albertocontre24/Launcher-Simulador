using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Launcher.Core.Services;

namespace Launcher.App;

public partial class App : Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _provider = services.BuildServiceProvider();

        var main = _provider.GetRequiredService<MainWindow>();
        main.Show();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<IRemoteManifestService, RemoteManifestService>();
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_provider is not null)
        {
            _provider.Dispose();
            _provider = null;
        }

        base.OnExit(e);
    }
}

