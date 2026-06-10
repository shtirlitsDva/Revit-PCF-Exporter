using System;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;

using PcfExporter.ElementSource;
using PcfExporter.Model;

using Shared;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Writes TAP-CONNECTION records. A tap below the diameter limit is suppressed
    /// entirely (historical behavior).
    /// </summary>
    public static class TapsWriter
    {
        /// <summary>Tap referenced from a PCF_ELEM_TAP1..3 slot on the tapped element.</summary>
        public static StringBuilder WriteSpecificTap(Element element, string tapParameterName, ExportSession s)
        {
            var sb = new StringBuilder();
            try
            {
                var familyInstance = (FamilyInstance)element;
                XYZ elementOrigin = ((LocationPoint)familyInstance.Location).Point;
                string uniqueId = element.LookupParameter(tapParameterName).AsString();

                Element tappingElement = uniqueId != null ? s.Doc.GetElement(uniqueId) : null;
                if (tappingElement == null) return sb;

                var cons = new Cons(tappingElement);
                Connector tapConnector =
                    elementOrigin.DistanceTo(cons.Primary.Origin) > elementOrigin.DistanceTo(cons.Secondary.Origin)
                        ? cons.Secondary : cons.Primary;

                if (!ElementFilters.PassesDiameterLimit(tappingElement, s.Cfg)) return new StringBuilder();
                return TapConnection(tapConnector.Origin, tapConnector.Radius, s);
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(
                    "Tap error! An object in the Taps module returned: " + ex.Message + " " +
                    "Check if taps are correctly defined.", ex);
            }
        }

        /// <summary>Tap registered on the tapped element via the TAP element type (PCF_ELEM_TAPS).</summary>
        public static StringBuilder WriteGenericTap(Element tapped, Element tapping, ExportSession s)
        {
            try
            {
                var cons1 = new Cons(tapped);
                var tappingConnectors = MepUtils.GetALLConnectorsFromElements(tapping);

                Line line = Line.CreateBound(cons1.Primary.Origin, cons1.Secondary.Origin);
                XYZ projected = line.Project(tappingConnectors.First().Origin).XYZPoint;

                Connector tapConnector = tappingConnectors.MinBy(x => x.Origin.DistanceTo(projected));

                if (!ElementFilters.PassesDiameterLimit(tapping, s.Cfg)) return new StringBuilder();
                return TapConnection(tapConnector.Origin, tapConnector.Radius, s);
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(
                    "Tap error! An object in the Taps module returned: " + ex.Message + " " +
                    "Check if taps are correctly defined.", ex);
            }
        }

        private static StringBuilder TapConnection(XYZ origin, double radius, ExportSession s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    TAP-CONNECTION");
            sb.AppendLine($"    CO-ORDS {s.EW.FormatPoint(origin)} {s.EW.FormatBore(radius)}");
            return sb;
        }
    }
}
