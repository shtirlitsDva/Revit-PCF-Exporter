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

namespace MEPUtils.WriteAllParGUID
{
    [Transaction(TransactionMode.Manual)]
    class WriteAllParGuid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            FilteredElementCollector col = new FilteredElementCollector(doc);
            Element el = col.OfClass(typeof(PipingSystemType)).FirstElement();

            StringBuilder sb = new StringBuilder();

            foreach (Parameter par in el.ParametersMap)
            {
                if (par.Definition.Name.StartsWith("PCF"))
                {
                    sb.Append(par.Definition.Name);
                    sb.Append(" : ");
                    sb.Append(par.GUID.ToString());
                    sb.AppendLine();
                }
            }

            Output.WriteDebugFile(@"G:\Temp.txt", sb);

            return Result.Succeeded;
        }
    }
}
