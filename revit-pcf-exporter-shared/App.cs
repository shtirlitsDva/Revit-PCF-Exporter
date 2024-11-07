#region Header
#endregion // Header

using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PCF_Functions;
//using mySettings = PCF_Functions.Properties.Settings;
using PCF_Taps;
using System.Diagnostics;

namespace PCF_Exporter
{

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //[Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class App : IExternalApplication
    {
        public const string pcfExporterButtonToolTip = "Export piping data to PCF";
        public const string tapConnectionButtonToolTip = "Define a tap connection";
        //public const string supportSystemTypeToolTip = "Set system type for pipe support";

        //Method to get the button image
        BitmapImage NewBitmapImage(Assembly a, string imageName)
        {
            Stream s = a.GetManifestResourceStream(imageName);

            BitmapImage img = new BitmapImage();

            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();

            return img;
        }

        // get the absolute path of this assembly
        static string ExecutingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        // get ref to assembly
        Assembly exe = Assembly.GetExecutingAssembly();

        public Result OnStartup(UIControlledApplication application)
        {
            AddMenu(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void AddMenu(UIControlledApplication application)
        {
            //Assembly exe = Assembly.GetExecutingAssembly();

            RibbonPanel rvtRibbonPanel = application.CreateRibbonPanel("PCF Tools");
            PushButtonData data = new PushButtonData("PCFExporter", "PCF", ExecutingAssemblyPath, "PCF_Exporter.FormCaller");
            data.ToolTip = pcfExporterButtonToolTip;
            data.Image = NewBitmapImage(exe, "PCF_Exporter.ImgPcfExport16.png");
            data.LargeImage = NewBitmapImage(exe, "PCF_Exporter.ImgPcfExport32.png");
            PushButton pushButton = rvtRibbonPanel.AddItem(data) as PushButton;

            data = new PushButtonData("TAPConnection", "Taps", ExecutingAssemblyPath, "PCF_Exporter.TapsCaller");
            data.ToolTip = tapConnectionButtonToolTip;
            data.Image = NewBitmapImage(exe, "PCF_Exporter.ImgTapCon16.png");
            data.LargeImage = NewBitmapImage(exe, "PCF_Exporter.ImgTapCon32.png");
            pushButton = rvtRibbonPanel.AddItem(data) as PushButton;

            //data = new PushButtonData("SupportSystemType","SST",ExecutingAssemblyPath, "PCF_Exporter.SupportsCaller");
            //data.ToolTip = supportSystemTypeToolTip;
            //data.Image = NewBitmapImage(exe, "PCF_Functions.ImgSupports16.png");
            //data.LargeImage = NewBitmapImage(exe, "PCF_Functions.ImgSupports32.png");
            //pushButton = rvtRibbonPanel.AddItem(data) as PushButton;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class FormCaller : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DocumentManager.Instance.Initialize(commandData.Application.ActiveUIDocument.Document);
                Dark_PCF_Exporter_form fm = new Dark_PCF_Exporter_form(commandData, message);
                fm.ShowDialog();
                PCF_Exporter.Properties.Settings.Default.Save();
                fm.Close();
                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                throw;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class TapsCaller : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            DefineTapConnection dtc = new DefineTapConnection();
            Result result = dtc.defineTapConnection(commandData, ref message, elements);
            if (result == Result.Failed) return Result.Failed;
            else if (result == Result.Succeeded) return Result.Succeeded;
            else return Result.Cancelled;
        }
    }

    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //class SupportsCaller : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        SetSupportPipingSystem dtc = new SetSupportPipingSystem();
    //        Result result = dtc.Execute(commandData, ref message, elements);
    //        if (result == Result.Failed) return Result.Failed;
    //        else if (result == Result.Succeeded) return Result.Succeeded;
    //        else return Result.Cancelled;
    //    }
    //}
}
