#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Norsyn.CommandPalette.Model
{
    // The fixed category → colour spec (from the design handoff). Category order
    // is the declaration order here; the pane groups and sorts by it. Colours are
    // used only to tint the drawn monogram fallback — real commands ship their own
    // PNG icons, so this is a safety net, not the primary art.
    public static class PaletteCategory
    {
        // Declared in intended display order.
        private static readonly (string Name, string Hex)[] _spec =
        {
            ("Insulation",           "#2E8B8B"),
            ("Pipe & Geometry",      "#3A6EA5"),
            ("Instrumentation",      "#7A4FA3"),
            ("Piping Systems",       "#3C8C4A"),
            ("Parameters & Tagging", "#C67A2E"),
            ("Rooms, Levels & Docs", "#A8503C"),
            ("Family",               "#A03A7A"),
            ("Analysis & QA",        "#52667A"),
            ("Connectors",           "#B06028"),
            ("Supports",             "#5A6470"),
        };

        // Commands whose category is unknown land here — last, neutral slate.
        public const string Fallback = "Other";
        private const string FallbackHex = "#52667A";

        private static readonly Dictionary<string, int> _order =
            _spec.Select((s, i) => (s.Name, i))
                 .ToDictionary(t => t.Name, t => t.i, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> _hex =
            _spec.ToDictionary(s => s.Name, s => s.Hex, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<string> AllInOrder =>
            _spec.Select(s => s.Name).ToList();

        // Sort key for grouping. Known categories keep their spec order; unknown
        // ones sort after all known ones, alphabetically among themselves.
        public static int OrderOf(string category) =>
            _order.TryGetValue(category, out int i) ? i : _spec.Length;

        public static string HexOf(string category) =>
            _hex.TryGetValue(category, out string? hex) ? hex : FallbackHex;
    }
}
