using System.Windows;
using Haystack.Data;
using Haystack.Theming;

namespace Haystack;

public partial class App : Application
{
    // Single-instance plumbing. The named mutex is the OS-level "already
    // running" marker (its name vanishes with the process, so a crash can never
    // wedge future launches); the named event is how a second launch hands its
    // focus over to the running instance instead of silently dying. Both are
    // held for the app's lifetime.
    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _activateSignal;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // One instance only: the UI assumes it's the only view of the database,
        // and two windows editing the same records would silently fight.
        _instanceMutex = new Mutex(true, "K1Apps.Haystack.Instance", out var isFirstInstance);
        _activateSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "K1Apps.Haystack.Activate");

        if (!isFirstInstance)
        {
            // Bring the running instance forward and bow out.
            _activateSignal.Set();
            Shutdown();
            return;
        }

        // First instance: whenever a later launch signals, surface our window.
        ThreadPool.RegisterWaitForSingleObject(_activateSignal,
            (_, _) => Dispatcher.BeginInvoke(ActivateMainWindow),
            null, Timeout.Infinite, executeOnlyOnce: false);

        Database.Initialize();
        ThemeManager.ApplySaved();
        NativeChrome.Initialize();

        var window = new MainWindow();
        window.Show();
    }

    private void ActivateMainWindow()
    {
        if (MainWindow is not { } window) return;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
