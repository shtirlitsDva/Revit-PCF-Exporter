#nullable enable
using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Shared;

namespace PDFExporter.App
{
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
