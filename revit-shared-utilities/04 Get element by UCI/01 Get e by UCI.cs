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
        private const int precision = 3;

        public static Result ElementCoordinatesPCF(ExternalCommandData cData)
        {
            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;
            var items = selection.GetElementIds().Select(x => doc.GetElement(x));

            string message = string.Empty;
            foreach (Element e in items)
            {
                message += e.Name + "\n";

                Cons cons = mp.GetConnectors(e);
                message += PCF_Functions.EndWriter.WriteEP1(e, cons.Primary);
                message += PCF_Functions.EndWriter.WriteEP1(e, cons.Secondary);
            }

            Shared.BuildingCoder.BuildingCoderUtilities.InfoMsg(message);

            return Result.Succeeded;
        }

        internal static string PointStringMm(XYZ p, int precision)
        {
            return string.Concat(
                Math.Round(p.X.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Y.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Z.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")));
        }
    }
}
