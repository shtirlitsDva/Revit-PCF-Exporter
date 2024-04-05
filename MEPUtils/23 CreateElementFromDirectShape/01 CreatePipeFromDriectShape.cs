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

namespace MEPUtils.CreateElementFromDS
{
    [Transaction(TransactionMode.Manual)]
    class CreateElementFromDirectShape : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            //Get the selected elements from the active document
            //and check if it is a DirectShape
            var selectedIds = uidoc.Selection.GetElementIds();
            var selectedElements = selectedIds
                .Select(x => doc.GetElement(x))
                .Where(x => x is DirectShape);

            foreach (DirectShape ds in selectedElements)
            {
                //Get the geometry of the DirectShape
                //Check if there are two planar circular faces
                //If so, create a pipe with the same diameter
                //With the origin of the two faces as start and end points                //With the origin of the two faces as start and end points
                //Otherwise, show a message

                var ops = new Options()
                {
                    //View = doc.ActiveView,
                    //ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };
                GeometryElement geometry = ds.get_Geometry(ops);

                //Gather origin points
                List<XYZ> origins = new List<XYZ>();

                foreach (GeometryObject geoObj in geometry)
                {
                    if (geoObj is Solid solid)
                    {
                        if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) { continue; }
                        // Get the faces
                        foreach (Face face in solid.Faces)
                        {
                            if (!(face is PlanarFace pFace)) { continue; }
                            origins.Add(pFace.Origin);
                        }
                    }
                }

                if (origins.Count == 2)
                {
                    PipeCreator.CreatePipeFromTwoPoints(uiApp, origins);
                }
            }

            return Result.Succeeded;
        }
    }
}
