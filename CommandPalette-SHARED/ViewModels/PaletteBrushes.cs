#nullable enable
using System.Collections.Generic;
using System.Windows.Media;

using Norsyn.CommandPalette.Model;

namespace Norsyn.CommandPalette.ViewModels
{
    // Frozen, cached category brushes derived from the fixed colour spec. Frozen
    // brushes are cheap to share across many rows.
    public static class PaletteBrushes
    {
        private static readonly Dictionary<string, Brush> _cache =
            new Dictionary<string, Brush>();
        private static readonly object _lock = new object();

        public static Brush ForCategory(string category)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(category, out var brush)) return brush;
                var b = new SolidColorBrush(Parse(PaletteCategory.HexOf(category)));
                b.Freeze();
                _cache[category] = b;
                return b;
            }
        }

        private static Color Parse(string hex)
        {
            var obj = ColorConverter.ConvertFromString(hex);
            return obj is Color c ? c : Colors.Gray;
        }
    }
}
