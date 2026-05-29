using System.Windows;

namespace HabitatRural;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services.DatabaseService.Instance.Initialize();
    }
}
