#nullable enable
using System;
using System.Reflection;

namespace Norsyn.CommandPalette.Model
{
    // One command as the pane sees it: flat, host-neutral, immutable data built
    // from a [DevReloadButton] scan plus the assembly that registered it. Favorite
    // state lives in the view-model / store, NOT here — this is pure identity.
    //
    // Plain class (no record / init accessors) so the same source compiles for the
    // net48 year-builds too, which lack IsExternalInit.
    public sealed class PaletteCommand
    {
        public PaletteCommand(
            string fullClassName,
            Assembly owningAssembly,
            string source,
            string label,
            string? tooltip,
            string category,
            string? icon16,
            string? icon32)
        {
            FullClassName = fullClassName;
            OwningAssembly = owningAssembly;
            Source = source;
            Label = label;
            Tooltip = tooltip;
            Category = category;
            Icon16 = icon16;
            Icon32 = icon32;
        }

        // Stable identity across sessions and the favorites key: "<Source>|<class>".
        public string Id => $"{Source}|{FullClassName}";

        public string FullClassName { get; }
        // The assembly to instantiate the IExternalCommand from at run time, and to
        // pull the embedded icon PNG out of.
        public Assembly OwningAssembly { get; }
        public string Source { get; }        // owning add-in display name
        public string Label { get; }
        public string? Tooltip { get; }
        public string Category { get; }
        public string? Icon16 { get; }       // embedded-resource suffix, e.g. "ImgMUFlanges16.png"
        public string? Icon32 { get; }

        // Two upper-case initials for the drawn monogram fallback when no PNG loads.
        public string Monogram
        {
            get
            {
                string s = (Label ?? "").Trim();
                if (s.Length == 0) return "?";
                string[] parts = s.Split(new[] { ' ', '-', '_', '&', '(' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
                return s.Length >= 2
                    ? s.Substring(0, 2).ToUpperInvariant()
                    : char.ToUpperInvariant(s[0]).ToString();
            }
        }
    }
}
