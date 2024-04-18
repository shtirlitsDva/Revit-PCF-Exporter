using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

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
using System.Diagnostics;
using Autodesk.Revit.DB.Architecture;
using sl = Shared.SimpleLogger;

namespace MEPUtils.WriteRoomNumbersToContainedElements
{
    [Transaction(TransactionMode.Manual)]
    class WriteRoomNumbersFromGenericElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            sl.clrLog();

            var els = fi.GetElementsWithConnectors(doc, true).ToHashSet();
            sl.log("Number of elements with connectors: " + els.Count);

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Reset MC System Codes");
                //Reset all MC System Codes
                foreach (var el in els)
                    el.LookupParameter("MC System Code")?.Set("");
                tx.Commit();
            }

            //Gather all generic elements from rooms
            var ges = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WherePasses(
                fi.ParameterValueGenericFilter(
                    doc, "ROOMS", BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS))
                .ToHashSet();

            using (Transaction tx = new Transaction(doc, "Determine room number!"))
            {
                tx.Start();
                try
                {
                    XYZ mid = default;

                    int failCount = 0;
                    int successCount = 0;

                    foreach (var el in els)
                    {
                        if (el is Pipe pipe)
                        {
                            Cons cons = new Cons(el);
                            mid = (cons.Primary.Origin + cons.Secondary.Origin) / 2;
                        }
                        else if (el is FamilyInstance fi)
                        {
                            mid = ((LocationPoint)fi.Location).Point;
                        }

                        if (mid == null) continue;

                        var query = ges.Where(x => IsPointInElement(x, mid));

                        if (query.Count() == 0) { failCount++; continue; }
                        if (query.Count() > 1) 
                        { 
                            sl.log("More than one generic element found for element: " + el.Id.IntegerValue);
                            failCount++; continue; 
                        }
                        Element ge = query.FirstOrDefault();
                        if (ge == null) { failCount++; continue; }
                        else successCount++;

                        Parameter parDestination = el.LookupParameter("MC System Code");
                        Parameter parSource = ge.LookupParameter("MC System Code");
                        if (parDestination == null || parSource == null) continue;

                        parDestination.Set(parSource.AsString());
                    }

                    sl.log("Success: " + successCount);
                    sl.log("Fails: " + failCount);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    Debug.WriteLine(ex.ToString());
                    throw;
                }
                tx.Commit();
            }

            return Result.Succeeded;
        }

        private static bool IsPointInElement(Element element, XYZ point)
        {
            //Modified from here:
            //https://forums.autodesk.com/t5/revit-api-forum/hot-to-knows-if-a-point-is-inside-a-mass-and-or-solid/m-p/8995187/highlight/true#M40967

            SolidCurveIntersectionOptions sco = new SolidCurveIntersectionOptions();
            sco.ResultType = SolidCurveIntersectionMode.CurveSegmentsInside;

            XYZ vector = XYZ.BasisZ * 100000;
            Line line = Line.CreateBound(point, point.Add(vector));
            double tolerance = 0.000001;

            Options opts = new Options();
            GeometryElement ge = element.get_Geometry(opts);
            foreach (GeometryObject geomObj in ge)
            {
                Solid solid = geomObj as Solid;
                if (solid == null) continue;

                SolidCurveIntersection sci = solid.IntersectWithCurve(line, sco);
                for (int i = 0; i < sci.SegmentCount; i++)
                {
                    Curve c = sci.GetCurveSegment(i);
                    if (point.IsAlmostEqualTo(c.GetEndPoint(0), tolerance)) return true;
                }
            }
            return false;
        }
    }
}
