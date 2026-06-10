using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PcfExporter.UI
{
    /// <summary>
    /// Styles the Win32 title bar (which XAML cannot reach) to the dark theme via
    /// DWM. Colors are resolved from Theme.xaml so the palette stays single-sourced.
    /// Requires Windows 11 — the only OS in use (user decision 2026-06-10); any
    /// DWM failure throws rather than leaving a half-styled window.
    /// </summary>
    public static class DarkTitleBar
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        /// <summary>Apply when the native handle exists; safe to call from the ctor.</summary>
        public static void Apply(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
                ApplyNow(window);
            else
                window.SourceInitialized += (s, e) => ApplyNow(window);
        }

        private static void ApplyNow(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            int darkMode = 1;
            Set(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, darkMode);
            Set(hwnd, DWMWA_CAPTION_COLOR, ToColorRef(ThemeColor(window, "BackgroundDeep")));
            Set(hwnd, DWMWA_TEXT_COLOR, ToColorRef(ThemeColor(window, "TextPrimary")));
        }

        private static void Set(IntPtr hwnd, int attribute, int value)
        {
            int hr = DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
            if (hr != 0)
                throw new InvalidOperationException(
                    $"DwmSetWindowAttribute({attribute}) failed with HRESULT 0x{hr:X8} — " +
                    "title bar could not be styled. This requires Windows 11.");
        }

        private static Color ThemeColor(Window window, string brushKey)
        {
            if (window.TryFindResource(brushKey) is SolidColorBrush brush) return brush.Color;
            throw new InvalidOperationException(
                $"Theme.xaml does not define SolidColorBrush '{brushKey}' — cannot style the title bar.");
        }

        //COLORREF is 0x00BBGGRR, the reverse of WPF's RGB order.
        private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
