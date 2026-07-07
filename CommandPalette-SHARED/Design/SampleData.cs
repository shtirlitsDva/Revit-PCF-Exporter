#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Norsyn.CommandPalette.Model;
using Norsyn.CommandPalette.Services;
using Norsyn.CommandPalette.ViewModels;

namespace Norsyn.CommandPalette.Design
{
    // The 38 sample commands from the design handoff. Used for the VS designer
    // and a standalone preview so the pane can be judged before it touches Revit.
    // Icons resolve to null here → the monogram fallback is what you see.
    public static class SampleData
    {
        // (Label, Category, Source)
        private static readonly (string Label, string Category, string Source)[] _rows =
        {
            ("Create all insulation", "Insulation", "MEPUtils"),
            ("Delete all insulation", "Insulation", "MEPUtils"),
            ("Insulation settings", "Insulation", "MEPUtils"),
            ("Create flanges", "Pipe & Geometry", "MEPUtils"),
            ("Pipe from connector", "Pipe & Geometry", "MEPUtils"),
            ("Move to distance", "Pipe & Geometry", "MEPUtils"),
            ("Element from DirectShape", "Pipe & Geometry", "MEPUtils"),
            ("Create instrument", "Instrumentation", "MEPUtils"),
            ("Create instrument NN", "Instrumentation", "MEPUtils"),
            ("Add PS view-filters", "Piping Systems", "MEPUtils"),
            ("Isolate selected PS", "Piping Systems", "MEPUtils"),
            ("Hide selected PS", "Piping Systems", "MEPUtils"),
            ("Select by GUID", "Piping Systems", "MEPUtils"),
            ("Create PS legend", "Piping Systems", "NTR Exporter"),
            ("Update abbreviation", "Piping Systems", "NTR Exporter"),
            ("Read link workset", "Piping Systems", "NTR Exporter"),
            ("(Re-)Number", "Parameters & Tagging", "MEPUtils"),
            ("Split parameter value", "Parameters & Tagging", "MEPUtils"),
            ("Copy flow data", "Parameters & Tagging", "MEPUtils"),
            ("Write all par GUIDs", "Parameters & Tagging", "MEPUtils"),
            ("Copy PST params", "Parameters & Tagging", "MEPUtils"),
            ("Set & increment", "Parameters & Tagging", "MEPUtils"),
            ("Set from ME", "Parameters & Tagging", "MEPUtils"),
            ("Assign correct levels", "Rooms, Levels & Docs", "MEPUtils"),
            ("Copy to another doc", "Rooms, Levels & Docs", "MEPUtils"),
            ("Write room numbers", "Rooms, Levels & Docs", "MEPUtils"),
            ("Rooms from link", "Rooms, Levels & Docs", "MEPUtils"),
            ("Room nums generic", "Rooms, Levels & Docs", "MEPUtils"),
            ("Family add parameters", "Family", "MEPUtils"),
            ("Create family types", "Family", "MEPUtils"),
            ("Total length", "Analysis & QA", "PCF Exporter"),
            ("Count welds", "Analysis & QA", "PCF Exporter"),
            ("Pressure loss calc", "Analysis & QA", "MEPUtils"),
            ("Spindle orientation QA", "Analysis & QA", "MEPUtils"),
            ("Connect connectors", "Connectors", "PCF Exporter"),
            ("Tilkoblet check", "Connectors", "PCF Exporter"),
            ("Place supports", "Supports", "MEPUtils"),
            ("Support tools", "Supports", "MEPUtils"),
        };

        private static readonly string[] _favoriteLabels =
            { "(Re-)Number", "Count welds", "Create flanges", "Isolate selected PS", "Move to distance", "Copy flow data" };

        public static IReadOnlyList<PaletteCommand> Commands()
        {
            Assembly asm = typeof(SampleData).Assembly;
            return _rows.Select(r => new PaletteCommand(
                fullClassName: "Sample." + r.Label.Replace(" ", ""),
                owningAssembly: asm,
                source: r.Source,
                label: r.Label,
                tooltip: r.Label + " — sample command.",
                category: r.Category,
                icon16: null,
                icon32: null)).ToList();
        }

        // A ready-to-bind VM for the XAML designer preview. Favorites are seeded in
        // an isolated temp file so the designer never touches the real user config.
        public static PaletteViewModel DesignViewModel()
        {
            var favorites = DesignFavoritesStore();
            var vm = new PaletteViewModel(favorites, _ => { });
            vm.SetCommands(Commands());
            return vm;
        }

        public static FavoritesViewModel DesignFavoritesViewModel()
        {
            var favorites = DesignFavoritesStore();
            var commands = Commands();
            return new FavoritesViewModel(favorites, () => commands, _ => { });
        }

        private static FavoritesStore DesignFavoritesStore()
        {
            var favorites = new FavoritesStore(
                Path.Combine(Path.GetTempPath(), "norsyn-palette-design.json"));
            foreach (var c in Commands().Where(c => _favoriteLabels.Contains(c.Label)))
                if (!favorites.IsFavorite(c.Id)) favorites.Toggle(c.Id);
            return favorites;
        }
    }
}
