#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Norsyn.CommandPalette.ViewModels
{
    // Resolves a command's embedded PNG (by resource-name suffix, e.g.
    // "ImgMUFlanges32.png") out of its owning assembly into a WPF ImageSource.
    // Mirrors how the ribbon builder loads its icons. Returns null when there is
    // no icon (design-time sample data, or a command that ships none) — the view
    // then draws the coloured monogram fallback.
    public static class IconLoader
    {
        public static ImageSource? Load(Assembly? assembly, string? suffix)
        {
            if (assembly == null || string.IsNullOrWhiteSpace(suffix)) return null;
            try
            {
                string? name = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(suffix!, StringComparison.OrdinalIgnoreCase));
                if (name == null) return null;

                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null) return null;

                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = stream;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch
            {
                return null;
            }
        }
    }
}
