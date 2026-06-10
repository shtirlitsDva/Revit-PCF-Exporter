using System;
using System.Globalization;
using System.Text;

using Autodesk.Revit.DB;

using Shared;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Pure, culture-invariant text formatting for PCF records. No Revit document access,
    /// no configuration — every method is a function of its arguments only (unit-testable).
    /// </summary>
    public static class PcfFormat
    {
        private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

        /// <summary>Formats an XYZ (Revit internal feet) as "x y z" in millimetres.</summary>
        public static string PointMm(XYZ p, int decimals = 1) =>
            PointMm(p.X, p.Y, p.Z, decimals);

        /// <summary>Pure overload (no Revit types) — unit-testable outside Revit.</summary>
        public static string PointMm(double xFeet, double yFeet, double zFeet, int decimals = 1)
        {
            return string.Concat(
                Component(xFeet, decimals), " ",
                Component(yFeet, decimals), " ",
                Component(zFeet, decimals));
        }

        private static string Component(double feet, int decimals)
        {
            string format = "0." + new string('0', Math.Max(1, decimals));
            return Math.Round(feet.FtToMm(), decimals, MidpointRounding.AwayFromZero)
                .ToString(format, Gb);
        }

        /// <summary>One keyworded attribute line, four-space indented: "    KEYWORD value".</summary>
        public static StringBuilder AttributeLine(string keyword, string value)
        {
            var sb = new StringBuilder();
            sb.Append("    ").Append(keyword).Append(' ').Append(value).AppendLine();
            return sb;
        }
    }
}
