using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using MoreLinq;
using System.Text;
using Autodesk.Revit.UI;
using Shared;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;
using fi = Shared.Filter;
using op = Shared.Output;
using mp = Shared.MepUtils;
//using mySettings = GeneralStability.Properties.Settings;

namespace MEPUtils
{
    public class TotalLineLength
    {
        public static Result TotalLineLengths(UIApplication uiApp)
        {
            var app = uiApp;
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;
            var selection = uiDoc.Selection.GetElementIds();
            var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));

            double totalLength = 0;

            foreach (Element el in elements)
            {
                if (el == null)
                {
                    Shared.BuildingCoder.BuildingCoderUtilities.ErrorMsg("One of the selected elements is null.");
                    break;
                }

                switch (el)
                {
                    case DetailCurve dc:
                        totalLength += dc.GeometryCurve.Length;
                        break;
                    case ModelCurve mc:
                        totalLength += mc.GeometryCurve.Length;
                        break;
                    case Pipe pipe:
                        Parameter par = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                        totalLength += par.AsDouble();
                        break;
                    default:
                        Shared.BuildingCoder.BuildingCoderUtilities.ErrorMsg(el.Name.ToString() + " is not implemented!");
                        break;
                }
            }

            Shared.BuildingCoder.BuildingCoderUtilities.InfoMsg(totalLength.FtToMm().Round(4).ToString());

            return Result.Succeeded;
        }
    }
}

