#nullable enable
using System;

namespace Shared
{
    /// <summary>
    /// Ribbon-button metadata for an addin's commands. Read BY NAME via
    /// reflection by two renderers: the DevReload host (dev-time hot-reload
    /// ribbon, "DevReload" tab) and the NorsynApps standalone reflector
    /// (release ribbon, "Norsyn" tab).
    ///
    /// This is the SINGLE source of the attribute, hosted in the
    /// revit-shared-utilities-shared shared project. Because that project is a
    /// Shared Project (.shproj) — its source is compiled INTO each addin, not
    /// referenced as a binary — every addin still gets its OWN compiled copy of
    /// this type. That is exactly what the by-name reflection matching requires
    /// (the attribute instance may live in a foreign AssemblyLoadContext during
    /// hot-reload, so it is never matched by type identity): there is still no
    /// shared *contract assembly*, only shared *source*. Do not rename the type
    /// — the scanner matches on the literal name "DevReloadButtonAttribute".
    ///
    /// Icons are embedded-resource name suffixes; both are optional
    /// (text-only buttons are legal).
    ///
    /// Grouping: buttons sharing a <see cref="Group"/> collapse into one
    /// container button (a "category flyout"); <see cref="GroupKind"/> selects
    /// "Pulldown" (default) or "Split".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DevReloadButtonAttribute : Attribute
    {
        public string? Text { get; set; }
        public string? Tooltip { get; set; }
        public string? LongDescription { get; set; }
        public string? Icon16 { get; set; }
        public string? Icon32 { get; set; }
        public string? Panel { get; set; }
        public string? Group { get; set; }
        public string? GroupKind { get; set; }
        public string? Stack { get; set; }
        public bool SeparatorBefore { get; set; }
        public bool SlideOut { get; set; }
        public int Order { get; set; }
    }
}
