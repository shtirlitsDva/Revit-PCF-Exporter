#region Namespaces
using System;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
//using adWin = Autodesk.Windows;
using MEPUtils.SharedStaging;
using cn = MEPUtils.ConnectConnectors;
using ped = MEPUtils.PED.InitPED;
using mep = MEPUtils.MEPUtilsClass;
using pdf = PDFExporter.PDFExporter;
using sup = MEPUtils.PlaceSupport.PlaceSupport;
using NLog;

#endregion

namespace MyRibbonPanel
{
    [Transaction(TransactionMode.Manual)]
    class App : IExternalApplication
    {
        public const string myRibbonPanelToolTip = "My Own Ribbon Panel";

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
            RibbonPanel rvtRibbonPanel = application.CreateRibbonPanel("MyRevitAddins");

            //ConnectConnectors
            PushButtonData data = new PushButtonData("ConnectConnectors", "Cons", ExecutingAssemblyPath,
                "MyRibbonPanel.ConnectConnectors");
            data.ToolTip = 
@"Zero elements selected -> Connect ALL unconnected connectors

One element selected -> Connect the element to adjacent
                        elements

One element selected + CTRL -> Disconnect the element

One element selected + SHIFT -> Connect a special pipe accessory
                                support if placed at connection

Two elements selected -> If disconnected - connect
                         If connected - disconnect

More than two elements selected + CTRL
                         -> Disconnect all selected elements";
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgConnectConnectors16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgConnectConnectors32.png");
            PushButton connectCons = rvtRibbonPanel.AddItem(data) as PushButton;

            //PipeInsulationVisibility
            data = new PushButtonData("PipeInsulationVisibility", "Visible", ExecutingAssemblyPath, "MyRibbonPanel.PipeInsulationVisibility");
            data.ToolTip = "Toggle visibility of pipe insulation in current view.";
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPipeInsulationVisibility16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPipeInsulationVisibility32.png");
            PushButton pipeInsulationVisibility = rvtRibbonPanel.AddItem(data) as PushButton;

            //PlaceSupports
            data = new PushButtonData("PlaceSupports", "Supports", ExecutingAssemblyPath, "MyRibbonPanel.PlaceSupports");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPlaceSupport16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPlaceSupport32.png");
            PushButton placeSupports = rvtRibbonPanel.AddItem(data) as PushButton;

            //PED
            data = new PushButtonData("PED", "PED", ExecutingAssemblyPath, "MyRibbonPanel.PEDclass");
            data.ToolTip = "LMB: Append PED data.\nCtrl+LMB: Overwrite PED data.";
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPED16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPED32.png");
            PushButton PED = rvtRibbonPanel.AddItem(data) as PushButton;

            //MEPUtils
            data = new PushButtonData("MEPUtils", "MEP", ExecutingAssemblyPath, "MyRibbonPanel.MEPUtilsCaller");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgMEPUtils16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgMEPUtils32.png");
            PushButton MEPUtils = rvtRibbonPanel.AddItem(data) as PushButton;

            //PDFExporter
            data = new PushButtonData("PDFExport", "PDF", ExecutingAssemblyPath, "MyRibbonPanel.PDFExport");
            data.ToolTip = "Exports selected sheet set to PDF.\nRequires BlueBeam";
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPDF16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPDF32.png");
            PushButton PDFExporter = rvtRibbonPanel.AddItem(data) as PushButton;

            //PED
            data = new PushButtonData("Mark", "Mark", ExecutingAssemblyPath, "MyRibbonPanel.SetMarkCaller");
            data.ToolTip = "Sets the mark of selected element(s).";
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgSetMark16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgSetMark32.png");
            PushButton SetMark = rvtRibbonPanel.AddItem(data) as PushButton;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class ConnectConnectors : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (Transaction trans = new Transaction(commandData.Application.ActiveUIDocument.Document))
                {
                    trans.Start("Connect the Connectors!");
                    cn.ConnectTheConnectors(commandData);
                    trans.Commit();
                }
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

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PipeInsulationVisibility : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (Transaction trans = new Transaction(commandData.Application.ActiveUIDocument.Document))
                {
                    trans.Start("Toggle Pipe Insulation visibility!");
                    MEPUtils.PipeInsulationVisibility.TogglePipeInsulationVisibility(commandData);
                    trans.Commit();
                }
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

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PlaceSupports : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = sup.StartPlaceSupportsProcedure(commandData);
            return result;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PEDclass : IExternalCommand
    {
        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            #region LoggerSetup
            //Nlog configuration
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            //Targets
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "G:\\GitHub\\log.txt", DeleteOldFileOnStartup = true };
            //Rules
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            //Apply config
            NLog.LogManager.Configuration = nlogConfig;
            //Throw Exceptions
            //LogManager.ThrowExceptions = true;
            //DISABLE LOGGING
            NLog.LogManager.DisableLogging();
            #endregion
            Logger log = LogManager.GetLogger("Log");
            log.Info("Logging started!");

            using (TransactionGroup txGp = new TransactionGroup(doc))
            {
                txGp.Start("Initialize PED data");

                //Pipes wallthickness is calculated directly in the schedule

                using (Transaction trans3 = new Transaction(doc))
                {
                    trans3.Start("Populate Olets");
                    ped ped = new ped();
                    ped.processOlets(commandData);
                    trans3.Commit();
                }

                txGp.Assimilate();
            }

            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class MEPUtilsCaller : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = mep.FormCaller(commandData);
            return result;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PDFExport : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = pdf.ExportPDF(commandData);
            return result;
        }
    }

    //[Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //class UICusomizationCaller : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Result result = RevitUICustomization.RevitUICustomization.test(commandData);
    //        return result;
    //    }
    //}

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class SetMarkCaller : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = MEPUtils.SetMark.SetMark.SetMarkExecute(commandData);
            return result;
        }
    }
}

