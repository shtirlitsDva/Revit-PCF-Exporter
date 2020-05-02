using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;

namespace Shared.Tools
{
    class GetElementByUCI
    {
        public static Result GetEByUCI(ExternalCommandData cData)
        {
            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            //Ask for a UCI input
            InputBoxBasic ds = new InputBoxBasic();
            ds.ShowDialog();

            Element element = doc.GetElement(ds.Text);
            List<ElementId> ids = new List<ElementId>(1);
            ids.Add(element.Id);
            
            Selection selection = uidoc.Selection;
            selection.SetElementIds(ids);

            return Result.Succeeded;
        }
    }
}
