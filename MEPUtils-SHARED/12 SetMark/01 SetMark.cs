using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MoreLinq;
using System;
using System.Linq;
using Shared;

namespace MEPUtils.SetMark
{
    public static class SetMark
    {
        public static Result SetMarkExecute(ExternalCommandData commandData)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;
            var selIds = selection.GetElementIds();
            if (selIds.Count == 0) throw new Exception("Empty selection: must select element(s) to set values first!");

            Element elToSet = selIds.Select(x => doc.GetElement(x)).FirstOrDefault();

            if (elToSet == null) throw new Exception("Failed to get an element! doc.GetElement returned null!");

            Parameter parTag1 = elToSet.get_Parameter(new Guid("a93679f7-ca9e-4a1e-bb44-0d890a5b4ba1"));
            Parameter parTag2 = elToSet.get_Parameter(new Guid("3b2afba4-447f-422a-8280-fd394718ad4e"));
            Parameter parTag3 = elToSet.get_Parameter(new Guid("5c238fab-f1b0-4946-9c92-c3037b8d3b68"));

            if (parTag1 == null || parTag2 == null || parTag3 == null) throw new Exception("Tag# parameters are not imported!");

            string exVal1 = parTag1.AsString();
            if (exVal1.IsNullOrEmpty()) exVal1 = "";

            string exVal2 = parTag2.AsString();
            if (exVal2.IsNullOrEmpty()) exVal2 = "";

            string exVal3 = parTag3.AsString();
            if (exVal3.IsNullOrEmpty()) exVal3 = "";

            //Type in the value to set
            InputBoxBasic ds = new InputBoxBasic();
            ds.Tag1Value = exVal1;
            ds.Tag2Value = exVal2;
            ds.Tag3Value = exVal3;
            ds.ShowDialog();

            using (Transaction t1 = new Transaction(doc))
            {
                t1.Start("Set Mark value!");
                parTag1.Set(ds.Tag1Value);
                parTag2.Set(ds.Tag2Value);
                parTag3.Set(ds.Tag3Value);
                t1.Commit();
            }

            return Result.Succeeded;
        }
    }
}