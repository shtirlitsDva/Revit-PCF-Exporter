using System;
using System.Text;

using Autodesk.Revit.DB;

using PcfExporter.Configuration;

using Shared;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Writes the geometric record lines of PCF components (END-POINT, BRANCH1-POINT,
    /// CENTRE-POINT, CO-ORDS). One private core composes the line; the public overloads
    /// keep the historical, easy-to-call shapes so element classes stay readable.
    /// Unit selection comes from the injected configuration.
    /// </summary>
    public sealed class EndpointWriter
    {
        private readonly PcfConfiguration _cfg;

        public EndpointWriter(PcfConfiguration cfg) => _cfg = cfg;

        #region Core
        /// <summary>Formats a position in the configured coordinate units.</summary>
        public string FormatPoint(XYZ p) => Point(p);
        /// <summary>Formats a connector radius as nominal bore in the configured bore units.</summary>
        public string FormatBore(double radius) => Bore(radius);

        private string Point(XYZ p) =>
            _cfg.CoordsUnits == CoordsUnits.Mm
                ? PcfFormat.PointMm(p)
                : Conversion.PointStringInch(p);

        private string Bore(double radius) =>
            _cfg.BoreUnits == BoreUnits.Mm
                ? Conversion.PipeSizeToMm(radius)
                : Conversion.PipeSizeToInch(radius);

        /// <summary>
        /// Composes one record line: keyword, position, optional bore, optional end-type text.
        /// </summary>
        private StringBuilder Write(string keyword, XYZ position, double? radius, string endType)
        {
            var sb = new StringBuilder();
            sb.Append("    ").Append(keyword).Append(' ').Append(Point(position));
            if (radius.HasValue) sb.Append(' ').Append(Bore(radius.Value));
            if (!string.IsNullOrEmpty(endType)) sb.Append(' ').Append(endType);
            sb.AppendLine();
            return sb;
        }

        private static string EndTypeOf(Element element, string parameterName) =>
            element?.LookupParameter(parameterName)?.AsString();
        #endregion

        #region END-POINT
        public StringBuilder WriteEP1(Element element, Connector connector) =>
            Write("END-POINT", connector.Origin, connector.Radius, EndTypeOf(element, "PCF_ELEM_END1"));

        public StringBuilder WriteEP2(Element element, Connector connector) =>
            Write("END-POINT", connector.Origin, connector.Radius, EndTypeOf(element, "PCF_ELEM_END2"));

        /// <summary>End-point 2 with the coordinates overridden (e.g. gasket offset).</summary>
        public StringBuilder WriteEP2(Element element, Connector connector, XYZ modifiedPosition) =>
            Write("END-POINT", modifiedPosition, connector.Radius, EndTypeOf(element, "PCF_ELEM_END2"));

        public StringBuilder WriteEP2(Element element, XYZ position, double radius) =>
            Write("END-POINT", position, radius, EndTypeOf(element, "PCF_ELEM_END2"));

        public StringBuilder WriteEP3(Element element, Connector connector) =>
            Write("END-POINT", connector.Origin, connector.Radius, EndTypeOf(element, "PCF_ELEM_END3"));
        #endregion

        #region BRANCH1-POINT
        public StringBuilder WriteBP1(Element element, Connector connector) =>
            Write("BRANCH1-POINT", connector.Origin, connector.Radius, EndTypeOf(element, "PCF_ELEM_BP1"));
        #endregion

        #region CENTRE-POINT
        public StringBuilder WriteCP(FamilyInstance familyInstance) =>
            Write("CENTRE-POINT", ((LocationPoint)familyInstance.Location).Point, null, null);

        public StringBuilder WriteCP(XYZ point) =>
            Write("CENTRE-POINT", point, null, null);

        /// <summary>
        /// Connector mid-point centre point. Historical quirk preserved: this overload
        /// has always been written with TWO decimals in mm mode (the old code routed it
        /// through a different formatter than the other point writers).
        /// </summary>
        public StringBuilder WriteCP(Connector primary, Connector secondary)
        {
            XYZ mid = (primary.Origin + secondary.Origin) / 2;
            string point = _cfg.CoordsUnits == CoordsUnits.Mm
                ? PcfFormat.PointMm(mid, 2)
                : Conversion.PointStringInch(mid);
            var sb = new StringBuilder();
            sb.Append("    CENTRE-POINT ").Append(point).AppendLine();
            return sb;
        }

        /// <summary>Tapping olet: centre point carries the tapped element's size and END1 type.</summary>
        public StringBuilder WriteTappingOletCP(Connector oletCpCon, Parameter end1, Element tappedElement)
        {
            string endType = end1?.AsString();
            double tappedRadius = new Cons(tappedElement).Primary.Radius;
            // Historical format: size and end type are separated by two spaces.
            var sb = new StringBuilder();
            sb.Append("    CENTRE-POINT ").Append(Point(oletCpCon.Origin))
              .Append(' ').Append(Bore(tappedRadius)).Append(' ');
            if (!string.IsNullOrEmpty(endType)) sb.Append(' ').Append(endType);
            sb.AppendLine();
            return sb;
        }
        #endregion

        #region CO-ORDS
        public StringBuilder WriteCO(XYZ point) =>
            Write("CO-ORDS", point, null, null);

        public StringBuilder WriteCO(FamilyInstance familyInstance) =>
            Write("CO-ORDS", ((LocationPoint)familyInstance.Location).Point, null, null);

        public StringBuilder WriteCO(FamilyInstance familyInstance, Connector sizeConnector) =>
            Write("CO-ORDS", ((LocationPoint)familyInstance.Location).Point, sizeConnector.Radius, null);
        #endregion
    }
}
