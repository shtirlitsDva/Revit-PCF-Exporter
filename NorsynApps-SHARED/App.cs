#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitDevReload;
using RevitDevReload.Core;

namespace NorsynApps
{
    // The release-mode ribbon host: a thin reflector. Addin projects are
    // added as VS project dependencies of the per-year NorsynApps project;
    // their DLLs land in this project's output folder, get scanned here for
    // [DevReloadButton]-attributed IExternalCommands (matched by name — the
    // same attribute the DevReload dev-time ribbon reads), and rendered onto
    // a "Norsyn" tab. Buttons point DIRECTLY at the addin dll + class:
    // release installs never hot-reload, so Revit's lazy default-context
    // load and file lock are exactly right here.
    //
    // Layout/scan/render logic is LINKED SOURCE from the DevReload repo
    // (RibbonDefinition.cs, RibbonBuilder.cs, CommandScanner.cs) — one
    // implementation, two renderers (dev tab + this).
    [Transaction(TransactionMode.Manual)]
    public sealed class NorsynAppsApp : IExternalApplication
    {
        public const string TabName = "Norsyn";

        private static readonly List<AddinAssembly> _addins = new();
        private static readonly List<string> _failures = new();

        internal static IReadOnlyList<AddinAssembly> Addins => _addins;
        internal static IReadOnlyList<string> Failures => _failures;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already exists (another Norsyn addin host) — share it.
            }

            // One bad addin must never take down the whole reflector: every
            // per-addin step is isolated and recorded.
            foreach (string dll in CandidateAddinFiles())
            {
                AddinAssembly? addin = TryScan(dll);
                if (addin == null) continue;
                _addins.Add(addin);
                try
                {
                    RenderAddin(application, addin);
                }
                catch (Exception ex)
                {
                    _failures.Add(
                        $"{addin.DisplayName}: ribbon rendering failed — {ex.Message}");
                }
            }

            CreateBrowserButton(application);

#if COMMANDPALETTE
            // Dockable command palette (first-til-mølle): register the pane, then
            // publish every scanned addin's [DevReloadButton] commands into it.
            // Compiled where COMMANDPALETTE is defined (every year that references
            // CommandPalette.Core — 2022/2024/2025).
            Norsyn.CommandPalette.CommandPalette.EnsurePane(application);
            foreach (AddinAssembly addin in _addins)
                Norsyn.CommandPalette.CommandPalette.Register(addin.Assembly);
            CreatePaletteButton(application);
#endif

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        // Only files whose assembly metadata references RevitAPIUI are addin
        // candidates — NuGet dependencies (CommunityToolkit, ExcelDataReader,
        // System.* shims on net48, …) are skipped WITHOUT loading them into
        // the session. PEReader touches metadata only; no code is loaded.
        private static IEnumerable<string> CandidateAddinFiles()
        {
            string selfPath = Assembly.GetExecutingAssembly().Location;
            string? folder = string.IsNullOrEmpty(selfPath)
                ? null : Path.GetDirectoryName(selfPath);

            // Location is empty when this assembly was byte-loaded into a collectible
            // ALC (e.g. loaded via DevReload rather than as a normal .addin). The
            // reflector's folder scan makes no sense there — skip it gracefully
            // instead of throwing, so the rest of OnStartup (incl. the palette) runs.
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                _failures.Add(
                    "Addin folder could not be located (no file path for this " +
                    "assembly — likely loaded via DevReload); reflector scan skipped.");
                yield break;
            }

            foreach (string dll in Directory.GetFiles(folder, "*.dll"))
            {
                if (string.Equals(dll, selfPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ReferencesRevitApiUi(dll))
                    yield return dll;
            }
        }

        private static bool ReferencesRevitApiUi(string dllPath)
        {
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var pe = new PEReader(stream);
                if (!pe.HasMetadata) return false;
                MetadataReader metadata = pe.GetMetadataReader();
                return metadata.AssemblyReferences
                    .Select(handle => metadata.GetAssemblyReference(handle))
                    .Any(reference => metadata.GetString(reference.Name)
                        .Equals("RevitAPIUI", StringComparison.OrdinalIgnoreCase));
            }
            catch (BadImageFormatException)
            {
                return false; // native dll — not an addin
            }
            catch (Exception ex)
            {
                _failures.Add($"{Path.GetFileName(dllPath)}: metadata probe failed — {ex.Message}");
                return false;
            }
        }

        private static AddinAssembly? TryScan(string dll)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var commands = CommandScanner.FindExternalCommands(assembly);
                if (commands.Count == 0) return null;
                var buttons = ButtonDefinitionScanner.Scan(assembly);
                return new AddinAssembly(assembly, dll, commands, buttons);
            }
            catch (Exception ex)
            {
                // It references RevitAPIUI, so it IS meant to be an addin —
                // a load failure here is a real problem worth surfacing.
                _failures.Add($"{Path.GetFileName(dll)}: load failed — {ex.Message}");
                return null;
            }
        }

        private static void RenderAddin(
            UIControlledApplication application, AddinAssembly addin)
        {
            if (addin.Buttons.Count == 0) return;

            foreach (PanelLayout layout in
                RibbonLayoutBuilder.Build(addin.Buttons, addin.DisplayName))
            {
                RibbonPanel panel = CreatePanelDisambiguated(
                    application, layout.Title, addin.DisplayName);
                RibbonBuilder.Render(
                    panel,
                    layout,
                    def => new PushButtonData(
                        $"{addin.DisplayName}.{def.FullClassName}",
                        def.Text,
                        addin.DllPath,
                        def.FullClassName),
                    suffix =>
                    {
                        var icon = RibbonBuilder.LoadEmbeddedIcon(addin.Assembly, suffix);
                        if (icon == null)
                            _failures.Add(
                                $"{addin.DisplayName}: declared icon '{suffix}' " +
                                "not found among embedded resources");
                        return icon;
                    });
            }
        }

        // Two addins declaring the same panel title (or a clash with the
        // reflector's own "Norsyn" panel) must not kill startup — prefix
        // with the addin name on collision.
        private static RibbonPanel CreatePanelDisambiguated(
            UIControlledApplication application, string title, string addinName)
        {
            try
            {
                return application.CreateRibbonPanel(TabName, title);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return application.CreateRibbonPanel(TabName, $"{addinName} {title}");
            }
        }

        private static void CreateBrowserButton(UIControlledApplication application)
        {
            RibbonPanel panel = application.CreateRibbonPanel(TabName, "Norsyn");
            var data = new PushButtonData(
                "NorsynApps.CommandBrowser",
                "All\nCommands",
                Assembly.GetExecutingAssembly().Location,
                typeof(CommandBrowserCommand).FullName)
            {
                ToolTip = "Browse and run any command from the installed " +
                          "Norsyn addins — including commands that have no " +
                          "ribbon button.",
            };
            panel.AddItem(data);
        }

#if COMMANDPALETTE
        // The ribbon button that shows/hides the dockable command palette. Also the
        // point where a live ExternalCommandData is captured for pane command runs.
        private static void CreatePaletteButton(UIControlledApplication application)
        {
            RibbonPanel panel = application.CreateRibbonPanel(TabName, "Palette");
            Type command = typeof(Norsyn.CommandPalette.Commands.ShowCommandPaletteCommand);
            var data = new PushButtonData(
                "NorsynApps.ShowCommandPalette",
                "Norsyn\nCommands",
                command.Assembly.Location,
                command.FullName)
            {
                ToolTip = "Show or hide the Norsyn Commands dockable palette — a " +
                          "docked home for every command that stays put no matter " +
                          "which ribbon tab is active.",
            };
            panel.AddItem(data);
        }
#endif
    }

    internal sealed class AddinAssembly
    {
        public AddinAssembly(
            Assembly assembly, string dllPath,
            IReadOnlyList<DiscoveredCommand> commands,
            IReadOnlyList<ButtonDefinition> buttons)
        {
            Assembly = assembly;
            DllPath = dllPath;
            Commands = commands;
            Buttons = buttons;
        }

        public Assembly Assembly { get; }
        public string DllPath { get; }
        public IReadOnlyList<DiscoveredCommand> Commands { get; }
        public IReadOnlyList<ButtonDefinition> Buttons { get; }
        public string DisplayName => Assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(DllPath);
    }
}
