#nullable enable
using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PDFExporter.App
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

    /// <summary>Exports the selected sheet set to PDF. Helper returns a Result.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "PDF",
        Tooltip = "Exports selected sheet set to PDF.\nRequires BlueBeam",
        Icon16 = "ImgPDF16.png", Icon32 = "ImgPDF32.png",
        Panel = "PDF", Order = 0)]
    public class PdfExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return PDFExporter.ExportPDF(commandData);
        }
    }
}
