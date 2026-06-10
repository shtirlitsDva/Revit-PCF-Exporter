using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PcfExporter.App
{
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        public const string PcfExporterButtonToolTip = "Export piping data to PCF";
        public const string TapConnectionButtonToolTip = "Define a tap connection";

        private static readonly string ExecutingAssemblyPath = Assembly.GetExecutingAssembly().Location;

        public Result OnStartup(UIControlledApplication application)
        {
            AddMenu(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        private static void AddMenu(UIControlledApplication application)
        {
            Assembly exe = Assembly.GetExecutingAssembly();
            RibbonPanel panel = application.CreateRibbonPanel("PCFE");

            var pcfData = new PushButtonData(
                "PCFExporter", "PCF", ExecutingAssemblyPath, typeof(PcfExporterCommand).FullName)
            {
                ToolTip = PcfExporterButtonToolTip,
                Image = EmbeddedImage(exe, "ImgPcfExport16.png"),
                LargeImage = EmbeddedImage(exe, "ImgPcfExport32.png")
            };
            panel.AddItem(pcfData);

            var tapsData = new PushButtonData(
                "TAPConnection", "Taps", ExecutingAssemblyPath, typeof(TapsCommand).FullName)
            {
                ToolTip = TapConnectionButtonToolTip,
                Image = EmbeddedImage(exe, "ImgTapCon16.png"),
                LargeImage = EmbeddedImage(exe, "ImgTapCon32.png")
            };
            panel.AddItem(tapsData);
        }

        private static BitmapImage EmbeddedImage(Assembly assembly, string imageNameSuffix)
        {
            //Resource names are prefixed with the (per-host) root namespace; match by suffix.
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(imageNameSuffix, StringComparison.OrdinalIgnoreCase)) continue;
                Stream stream = assembly.GetManifestResourceStream(name);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
                return image;
            }
            //A missing icon means the build or projitems broke — fail at startup, loudly.
            throw new InvalidOperationException(
                $"Embedded ribbon image '{imageNameSuffix}' was not found in {assembly.GetName().Name}.");
        }
    }

    /// <summary>Opens (or activates) the modeless PCF exporter window.</summary>
    [Transaction(TransactionMode.Manual)]
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
    public class TapsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return new DefineTapConnection().Execute(commandData, ref message);
        }
    }
}
