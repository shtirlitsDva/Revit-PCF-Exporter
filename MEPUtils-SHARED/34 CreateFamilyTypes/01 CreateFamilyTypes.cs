using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;

using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using Shared.Forms;
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
using Shared.BuildingCoder;
using System.Diagnostics;

namespace MEPUtils.CreateFamilyTypes
{
    [Transaction(TransactionMode.Manual)]
    class CreateFamilyTypes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            int[] dns = [250, 200, 150, 125, 100, 80, 65, 50, 40, 32, 25, 20];
            int[] ss = [1, 2, 3];


            var fec = new FilteredElementCollector(doc);
            var query = fec.OfClass(typeof(FamilySymbol))
                .Where(x => x.Name == "old");

            foreach (var item in query)
            {
                Debug.WriteLine(item.Name);
            }

            using var t = new Transaction(doc, "Set parameter value");

            t.Start();

            try
            {
                var et = query.FirstOrDefault() as ElementType;
                if (et == null) return Result.Failed;
                Debug.WriteLine(et.Name);
                foreach (var dn in dns)
                {
                    foreach (var s in ss)
                    {
                        var tname = $"{dn} S{s}";
                        var nt = et.Duplicate(tname);
                        if (nt == null) throw new Exception();
                        var p1 = nt.LookupParameter("DN");
                        p1.Set(dn);
                        var p2 = nt.LookupParameter("Serie");
                        p2.Set(s);
                    }
                }
            }
            catch (Exception)
            {
                t.RollBack();
                throw;
            }

            t.Commit();

            return Result.Succeeded;
        }
    }
}
