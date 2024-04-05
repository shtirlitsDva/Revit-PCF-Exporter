using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MEPUtils.SharedStaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;
using Autodesk.Revit.Attributes;
using NLog;

namespace MEPUtils.TestIExternalCommand
{
    [Transaction(TransactionMode.Manual)]
    public class TestNlog : IExternalCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("G:\\Github\\shtirlitsDva\\MyRevitAddins\\MyRevitAddins\\SetTagsModeless\\NLog.config");

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    log.Debug($"Test log iteration {i}");
                }
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            log.Debug($"Test log completed!");

            return Result.Succeeded;
        }
    }
}
