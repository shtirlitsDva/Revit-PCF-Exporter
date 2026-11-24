using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;

namespace Shared.Theme
{
    /// <summary>
    /// Central entry point for the Norsyn dark theme (dark mode only). Loads the
    /// embedded <c>Colors.xaml</c> + <c>Theme.xaml</c> at runtime with
    /// <see cref="XamlReader"/> (this also works in the .NET Framework Revit builds
    /// that have no XAML compile step) and applies them to a window, including a
    /// dark native title bar.
    ///
    /// <para>The dictionaries are embedded into every add-in assembly under stable
    /// logical names (see the shared .projitems). Because this class is compiled
    /// into each of those assemblies, <see cref="Assembly.GetExecutingAssembly"/>
    /// resolves to the caller's own assembly and finds its embedded copy.</para>
    ///
    /// <para>NOTE: this file compiles into C# 7.3 projects too. Keep the syntax
    /// C# 7.3 compatible.</para>
    /// </summary>
    public static class ThemeManager
    {
        private const string ThemeResourceName = "RevitShared.Theme.xaml";
        private const string ColorsResourceName = "RevitShared.Colors.xaml";

        private static ResourceDictionary _cached;

        /// <summary>The parsed theme dictionary (loaded once, then reused).</summary>
        public static ResourceDictionary Dictionary => _cached ?? (_cached = Load());

        /// <summary>
        /// Merges the theme into <paramref name="window"/>, gives the window the
        /// theme's Window style, and makes its title bar dark. Call once, before the
        /// window is shown (for a XAML window: before <c>InitializeComponent</c>, so
        /// that the view's <c>{StaticResource}</c> lookups of theme keys resolve).
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            window.Resources.MergedDictionaries.Add(Dictionary);

            // An implicit style in a window's own Resources is not applied to the
            // window itself, so assign the theme's Window style explicitly.
            window.Style = (Style)Dictionary[typeof(Window)];

            EnableDarkTitleBar(window);
        }

        /// <summary>
        /// A named brush from the theme, for controls built in code that need a
        /// semantic colour (e.g. the axis labels).
        /// </summary>
        /// <exception cref="KeyNotFoundException">The key is not a brush in Colors.xaml.</exception>
        public static Brush GetBrush(string key)
        {
            if (Dictionary[key] is Brush brush) return brush;
            throw new KeyNotFoundException($"The theme has no brush '{key}'. See Colors.xaml for the valid keys.");
        }

        /// <summary>A keyed style from the theme (e.g. "Button.Primary"), for controls built in code.</summary>
        /// <exception cref="KeyNotFoundException">The key is not a style in Theme.xaml.</exception>
        public static Style GetStyle(string key)
        {
            if (Dictionary[key] is Style style) return style;
            throw new KeyNotFoundException($"The theme has no style '{key}'. See Theme.xaml for the valid keys.");
        }

        private static ResourceDictionary Load()
        {
            // Colours live in their own dictionary (the single source of truth);
            // Theme.xaml holds only styles and refers to those colours with
            // {DynamicResource}. Merge the colours INTO the theme dictionary so the
            // deferred lookups resolve, and so GetBrush can reach them.
            ResourceDictionary colors = LoadDictionary(ColorsResourceName);
            ResourceDictionary theme = LoadDictionary(ThemeResourceName);
            theme.MergedDictionaries.Insert(0, colors);
            return theme;
        }

        private static ResourceDictionary LoadDictionary(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Embedded theme resource '{resourceName}' not found in {asm.GetName().Name}. " +
                        "Confirm the EmbeddedResource + <LogicalName> entry in the shared .projitems.");

                return (ResourceDictionary)XamlReader.Load(stream);
            }
        }

        // ------------------------------------------------------------------
        // Title bar
        // ------------------------------------------------------------------

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        /// <summary>
        /// Asks DWM to paint the title bar dark. On Windows 11 it also takes the
        /// caption, text and border colours from the palette, so the title bar matches
        /// the window. The handle exists only after SourceInitialized, so defer if needed.
        /// <para>These are cosmetic OS features: on a Windows version that does not
        /// support an attribute, DWM returns an error and the system title bar stays.
        /// That is the intended behaviour, not a hidden failure.</para>
        /// </summary>
        private static void EnableDarkTitleBar(Window window)
        {
            void Set()
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int enabled = 1;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref enabled, sizeof(int));

                SetColor(hwnd, DWMWA_CAPTION_COLOR, "Brush.Bg.Window");
                SetColor(hwnd, DWMWA_TEXT_COLOR, "Brush.Text.Primary");
                SetColor(hwnd, DWMWA_BORDER_COLOR, "Brush.Border");
            }

            if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
                Set();
            else
                window.SourceInitialized += (s, e) => Set();
        }

        private static void SetColor(IntPtr hwnd, int attribute, string brushKey)
        {
            var brush = GetBrush(brushKey) as SolidColorBrush;
            if (brush == null)
                throw new InvalidOperationException($"Theme brush '{brushKey}' must be a SolidColorBrush for the title bar.");

            Color c = brush.Color;
            int colorRef = c.R | (c.G << 8) | (c.B << 16); // COLORREF = 0x00BBGGRR
            DwmSetWindowAttribute(hwnd, attribute, ref colorRef, sizeof(int));
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
