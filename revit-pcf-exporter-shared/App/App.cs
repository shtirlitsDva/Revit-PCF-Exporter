#nullable enable
using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PcfExporter.App
{
    /// <summary>
    /// Ribbon-button metadata for this addin's commands. Read BY NAME via
    /// reflection by two renderers: the DevReload host (dev-time hot-reload
    /// ribbon, "DevReload" tab) and the NorsynApps standalone reflector
    /// (release ribbon, "Norsyn" tab). Each addin declares its own copy of
    /// this attribute — there is intentionally no shared contract assembly.
    /// Icons are embedded-resource name suffixes; both are optional
    /// (text-only buttons are legal).
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

    /// <summary>
    /// Lifecycle hooks — the Revit analog of AutoCAD's Initialize/Terminate.
    /// DevReload runs OnShutdown before every unload/reload, so everything a
    /// generation of this addin holds (the modeless window, its ExternalEvent
    /// executor, any future event subscriptions) must be released here; the
    /// next generation then starts clean instead of leaving a stale window
    /// running old code.
    /// </summary>
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            PcfWindowController.Shutdown();
            return Result.Succeeded;
        }
    }

    /// <summary>Opens (or activates) the modeless PCF exporter window.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "PCF",
        Tooltip = "Export piping data to PCF",
        Icon16 = "ImgPcfExport16.png", Icon32 = "ImgPcfExport32.png",
        Panel = "PCFE", Order = 0)]
    public class PcfExporterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                PcfWindowController.ShowOrActivate(commandData.Application);
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
        }
    }

    /// <summary>Interactive command: define a tap connection between two elements.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Taps",
        Tooltip = "Define a tap connection",
        Icon16 = "ImgTapCon16.png", Icon32 = "ImgTapCon32.png",
        Panel = "PCFE", Order = 1)]
    public class TapsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return new DefineTapConnection().Execute(commandData, ref message);
        }
    }
}
