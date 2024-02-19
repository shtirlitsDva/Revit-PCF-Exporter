#region Header
#endregion // Header

using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace NTR_Exporter
{

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //[Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class App : IExternalApplication
    {
        public const string ntrExporterButtonToolTip = "Export piping data to NTR";

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

            RibbonPanel rvtRibbonPanel = application.CreateRibbonPanel("NTR");
            PushButtonData data = new PushButtonData("NTRExporter", "NTR", ExecutingAssemblyPath, "NTR_Exporter.FormCaller")
            {
                ToolTip = ntrExporterButtonToolTip,
                Image = NewBitmapImage(exe, "NTR_Exporter.Resources.ImgNtrExport16.png"),
                LargeImage = NewBitmapImage(exe, "NTR_Exporter.Resources.ImgNtrExport32.png")
            };
            PushButton pushButton = rvtRibbonPanel.AddItem(data) as PushButton;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class FormCaller : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                NTR_Exporter_form fm = new NTR_Exporter_form(commandData, message);
                fm.ShowDialog();
                Properties.Settings.Default.Save();
                fm.Close();
                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
