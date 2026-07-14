using System.Windows;
using Haystack.Data;
using Haystack.Theming;

namespace Haystack;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Database.Initialize();
        ThemeManager.ApplySaved();
        NativeChrome.Initialize();

        var window = new MainWindow();
        window.Show();
    }
}
