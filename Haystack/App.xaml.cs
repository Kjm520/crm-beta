using System.Windows;
using Haystack.Data;

namespace Haystack;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Database.Initialize();

        var window = new MainWindow();
        window.Show();
    }
}
