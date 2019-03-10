#region Header
#endregion // Header

using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
//using mySettings = PCF_Functions.Properties.Settings;

namespace Shared
{

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //[Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class App : IExternalApplication
    {
        public const string analysisTools = "Analysis tools";

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

            RibbonPanel rvtRibbonPanel = application.CreateRibbonPanel("Tools");
            PushButtonData data = new PushButtonData("Tools","TLS",ExecutingAssemblyPath,"Shared.FormCaller");
            data.ToolTip = analysisTools;
            data.Image = NewBitmapImage(exe, "Shared.Resources.AnalysisTools16.png");
            data.LargeImage = NewBitmapImage(exe, "Shared.Resources.AnalysisTools32.png");
            PushButton pushButton = rvtRibbonPanel.AddItem(data) as PushButton;

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
                Result result = Shared.Tools.AnalysisTools.FormCaller(commandData);
                return result;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
