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
using log = Shared.SimpleLogger;
using Microsoft.Office.Interop.Excel;

namespace MEPUtils.PipingSystemsAndFilters
{
    [Transaction(TransactionMode.Manual)]
    class TestSpindleOrientation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            log.LogFileName = @"C:\Temp\DbgLog.txt";
            //log.clrLog();

            using (Transaction tr = new Transaction(doc, "Test spindle orientation"))
            {
                tr.Start();
                try
                {
                    Selection selection = uiApp.ActiveUIDocument.Selection;
                    ICollection<ElementId> elemIds = selection.GetElementIds();
                    if (elemIds == null) throw new Exception("Getting element from selection failed!");
                    if (elemIds.Count == 0) throw new Exception("No elements selected!");

                    var spDict = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_GenericModel)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(x => x.FamilyAndTypeName() == "Spindle direction: Spindle direction")
                        .ToDictionary(x => x.SuperComponent.Id, x => x);

                    foreach (ElementId elemId in elemIds)
                    {
                        if (spDict.ContainsKey(elemId))
                        {
                            FamilyInstance sd = spDict[elemId];
                            XYZ elementLocation = ((LocationPoint)sd.Location).Point;
                            Transform trf = sd.GetTransform();
                            XYZ direction = trf.BasisZ;

                            //dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Marker Line", 
                            //    elementLocation, elementLocation + direction.Normalize() * 3);

                            log.log(direction);
                            log.log(MapToCardinalDirection(direction));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    tr.RollBack();
                    throw;
                }
                tr.Commit();
            }

            return Result.Succeeded;
        }

        private string GetDirectionText(XYZ direction)
        {
            double tolerance = Math.Cos(Math.PI / 4); // 45 degrees tolerance
            direction = direction.Normalize();

            if (Math.Abs(direction.Z) > tolerance)
                return direction.Z > 0 ? "UP" : "DOWN";
            if (Math.Abs(direction.Y) > tolerance)
                return direction.Y > 0 ? "FRONT" : "BACK";
            if (Math.Abs(direction.X) > tolerance)
                return direction.X > 0 ? "RIGHT" : "LEFT";
            return "UNKNOWN";
        }

        private string MapToCardinalDirection(XYZ direction)
        {
            string viewCubeDirection = GetDirectionText(direction);
            switch (viewCubeDirection)
            {
                case "FRONT": return "NORTH";
                case "BACK": return "SOUTH";
                case "RIGHT": return "EAST";
                case "LEFT": return "WEST";
                case "UP": return "UP";
                case "DOWN": return "DOWN";
                default: return "UNKNOWN";
            }
        }
    }
}