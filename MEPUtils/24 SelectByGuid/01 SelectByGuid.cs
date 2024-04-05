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

namespace MEPUtils.SelectByGuid
{
    [Transaction(TransactionMode.Manual)]
    class SelectByGuid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            InputBox ib = new InputBox();
            ib.ShowDialog();

            if (!ib.Execute) return Result.Cancelled;

            if (ib.GUID.IsNoE()) return Result.Cancelled;

            var split = ib.GUID.Split(';');
            List<Element> list = new List<Element>();

            foreach (string s in split)
            {
                //Guid guid = default;
                //if (Guid.TryParse(s, out guid))
                //{
                    list.Add(doc.GetElement(s));
                //}
            }

            Selection selection = uidoc.Selection;

            selection.SetElementIds(list.Select(x => x.Id).ToList());
            uiApp.ActiveUIDocument.ShowElements(list.Select(x => x.Id).ToList());

            return Result.Succeeded;
        }
    }
}
