#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Norsyn.CommandPalette.Model;

using RevitDevReload.Core; // linked source: ButtonDefinitionScanner, ButtonDefinition

namespace Norsyn.CommandPalette.Registry
{
    // The single shared command registry. Lives in the CommandPalette.Core binary,
    // so every project that references Core sees the SAME instance — that is what
    // makes "each project registers its own commands" land in one place. Sources
    // push their assemblies in via Register(); the pane binds to Commands and
    // refreshes on Changed.
    public static class CommandRegistry
    {
        private static readonly object _lock = new object();
        // Keyed by assembly so a re-Register (e.g. a DevReload reload) cleanly
        // replaces that source's contribution, and Unregister removes it.
        private static readonly Dictionary<Assembly, IReadOnlyList<PaletteCommand>> _byAssembly =
            new Dictionary<Assembly, IReadOnlyList<PaletteCommand>>();

        // Raised (on the caller's thread) whenever the command set changes. The
        // pane subscribes and rebuilds; marshal to the UI dispatcher on the far side.
        public static event Action? Changed;

        public static IReadOnlyList<PaletteCommand> Commands
        {
            get
            {
                lock (_lock)
                    return _byAssembly.Values.SelectMany(c => c).ToList();
            }
        }

        // Scan one assembly for [DevReloadButton] commands and publish them under
        // that assembly's name. Idempotent per assembly.
        public static void Register(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            string source = assembly.GetName().Name ?? "Unknown";
            var commands = ButtonDefinitionScanner.Scan(assembly)
                .Select(b => ToPaletteCommand(b, assembly, source))
                .ToList();

            lock (_lock)
            {
                if (commands.Count == 0)
                {
                    // Nothing attributed — make sure any stale entry is gone.
                    if (!_byAssembly.Remove(assembly)) return;
                }
                else
                {
                    _byAssembly[assembly] = commands;
                }
            }
            Changed?.Invoke();
        }

        public static void Unregister(Assembly assembly)
        {
            bool removed;
            lock (_lock) removed = _byAssembly.Remove(assembly);
            if (removed) Changed?.Invoke();
        }

        private static PaletteCommand ToPaletteCommand(
            ButtonDefinition b, Assembly assembly, string source)
        {
            // Category = the ribbon Group (the flyout name) when present; flat
            // commands with no Group fall back to their Panel, then to "Other".
            // (Refinement flagged for later: a dedicated pane-category so flat
            // buttons can be categorised without folding them on the ribbon.)
            string category =
                !string.IsNullOrWhiteSpace(b.Group) ? b.Group! :
                !string.IsNullOrWhiteSpace(b.Panel) ? b.Panel! :
                PaletteCategory.Fallback;

            return new PaletteCommand(
                fullClassName: b.FullClassName,
                owningAssembly: assembly,
                source: source,
                label: b.Text,
                tooltip: b.Tooltip ?? b.LongDescription,
                category: category,
                icon16: b.Icon16,
                icon32: b.Icon32);
        }
    }
}
