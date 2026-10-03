using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Haystack.Theming;

/// <summary>
/// Keeps every window on Windows' native composition path (DWM) while still
/// looking like Haystack: chromeless, rounded, shadowed.
///
/// The windows used to fake rounded corners with AllowsTransparency plus a
/// rounded root Border. That is the legacy trick · and it opts a window out of
/// DWM entirely, which cost the native window shadow, the open/close/minimize
/// animations, and clean teardown (closing showed a stale flash of the window).
/// Asking DWM to round the corners itself keeps the look with none of the
/// costs: the OS draws the radius, the shadow, and the window border, and
/// animates the window like every other native app.
/// </summary>
public static class NativeChrome
{
    // DWMWA_WINDOW_CORNER_PREFERENCE (Windows 11+): 2 = DWMWCP_ROUND.
    private const int WindowCornerPreference = 33;
    private const int RoundedCorners = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Call once at startup. Rounds every window the app ever shows, via a class
    /// handler on Window.Loaded · the HWND exists by then, and the first frame
    /// hasn't been presented yet, so windows never appear square-cornered.
    /// </summary>
    public static void Initialize()
        => EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is not Window window) return;

                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == 0) return;

                var preference = RoundedCorners;
                // Best-effort: Windows 10 doesn't know the attribute and the call
                // simply fails, leaving square corners · everything else works.
                DwmSetWindowAttribute(hwnd, WindowCornerPreference, ref preference, sizeof(int));
            }));
}
