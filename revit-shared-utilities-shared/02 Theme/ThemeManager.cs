using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;

namespace Shared.Theme
{
    /// <summary>
    /// Central entry point for the Norsyn dark theme. Loads the embedded
    /// <c>Theme.xaml</c> (a loose <see cref="ResourceDictionary"/>, parsed at
    /// runtime with <see cref="XamlReader"/> so it works in the legacy .NET
    /// Framework Revit builds that have no XAML/BAML compile step) and applies it
    /// to a window, including a native dark title bar.
    ///
    /// <para>The dictionary is embedded into every add-in assembly under the
    /// stable logical name <see cref="ResourceName"/>; because this class is
    /// compiled into each of those assemblies, <see cref="Assembly.GetExecutingAssembly"/>
    /// resolves to the caller's own assembly and finds its embedded copy.</para>
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>Stable logical name pinned in the shared .projitems.</summary>
        private const string ResourceName = "RevitShared.Theme.xaml";

        private static ResourceDictionary _cached;

        /// <summary>The parsed theme dictionary (loaded once, then reused).</summary>
        public static ResourceDictionary Dictionary => _cached ?? (_cached = Load());

        /// <summary>
        /// Merges the theme into <paramref name="window"/> and switches its title
        /// bar to dark. Call once, before the window is shown.
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) return;

            window.Resources.MergedDictionaries.Add(Dictionary);

            // The implicit Window style is not reliably applied to the window that
            // owns the dictionary, so set the root surfaces explicitly.
            if (Dictionary["Brush.Bg.Window"] is Brush bg) window.Background = bg;
            if (Dictionary["Brush.Text.Primary"] is Brush fg) window.Foreground = fg;

            EnableDarkTitleBar(window);
        }

        /// <summary>
        /// A named brush from the theme, for controls built in code that need a
        /// semantic colour (e.g. the axis labels). Returns null if the key is absent.
        /// </summary>
        public static Brush GetBrush(string key) => Dictionary[key] as Brush;

        private static ResourceDictionary Load()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Embedded theme '{ResourceName}' not found in {asm.GetName().Name}. " +
                        "Confirm the EmbeddedResource + <LogicalName> entry in the shared .projitems.");

                return (ResourceDictionary)XamlReader.Load(stream);
            }
        }

        /// <summary>
        /// Asks DWM to paint the non-client area (title bar) dark. The window
        /// handle only exists once the source is initialised, so defer if needed.
        /// </summary>
        private static void EnableDarkTitleBar(Window window)
        {
            void Set()
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int enabled = 1;
                // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Win10 20H1+ / Win11).
                // 19 = the same flag on earlier Win10 builds. Try the modern one,
                // fall back to the legacy one; both are no-ops on unsupported OSes.
                if (DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref enabled, sizeof(int));
            }

            if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
                Set();
            else
                window.SourceInitialized += (s, e) => Set();
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
