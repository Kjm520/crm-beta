using System.Windows;
using PulsePoint.Data;

namespace PulsePoint;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Database.Initialize();

        var window = new MainWindow();
        window.Show();
    }
}
