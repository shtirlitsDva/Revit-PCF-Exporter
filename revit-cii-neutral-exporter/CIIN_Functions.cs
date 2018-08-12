using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms.ComponentModel.Com2Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;
using MoreLinq;
using iv = CIINExporter.InputVars;
using pdef = CIINExporter.ParameterDefinition;
using plst = CIINExporter.ParameterList;
using Autodesk.Revit.DB.Mechanical;
using static CIINExporter.Enums;

namespace CIINExporter
{
    public class InputVars
    {
        #region Execution
        //Used for "global variables".
        //File I/O
        public static string OutputDirectoryFilePath;
        public static string ExcelSheet = "COMP";

        //Execution control
        public static bool ExportAllOneFile = true;
        public static bool ExportAllSepFiles = false;
        public static bool ExportSpecificPipeLine = false;
        public static bool ExportSelection = false;
        public static double DiameterLimit = 0;
        public static bool WriteWallThickness = false;
        public static bool ExportToPlant3DIso = false;
        public static bool ExportToCII = false;
        public static bool Overwrite = true;

        //PCF File Header (preamble) control
        public static string UNITS_BORE = "MM";
        public static bool UNITS_BORE_MM = true;
        public static bool UNITS_BORE_INCH = false;

        public static string UNITS_CO_ORDS = "MM";
        public static bool UNITS_CO_ORDS_MM = true;
        public static bool UNITS_CO_ORDS_INCH = false;

        public static string UNITS_WEIGHT = "KGS";
        public static bool UNITS_WEIGHT_KGS = true;
        public static bool UNITS_WEIGHT_LBS = false;

        public static string UNITS_WEIGHT_LENGTH = "METER";
        public static bool UNITS_WEIGHT_LENGTH_METER = true;
        //public static bool UNITS_WEIGHT_LENGTH_INCH = false; OBSOLETE
        public static bool UNITS_WEIGHT_LENGTH_FEET = false;
        #endregion Execution

        #region Filters
        //Filters
        public static string SysAbbr = "FVF";
        public static BuiltInParameter SysAbbrParam = BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM;
        public static string PipelineGroupParameterName = "System Abbreviation";
        #endregion Filters

        #region Element parameter definition
        //Shared parameter group
        //public const string PCF_GROUP_NAME = "PCF"; OBSOLETE
        public const BuiltInParameterGroup PCF_BUILTIN_GROUP_NAME = BuiltInParameterGroup.PG_ANALYTICAL_MODEL;

        //PCF specification - OBSOLETE
        //public static string PIPING_SPEC = "STD";
        #endregion
    }

    public class Filter
    {
        public static ElementParameterFilter ParameterValueFilterStringEquals(string valueQualifier, BuiltInParameter testParam)
        {
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)testParam));
            FilterStringRuleEvaluator str = new FilterStringEquals();
            FilterStringRule paramFr = new FilterStringRule(pvp, str, valueQualifier, false);
            return new ElementParameterFilter(paramFr);
        }

        public static FilteredElementCollector GetElementsWithConnectors(Document doc)
        {
            // what categories of family instances
            // are we interested in?
            // From here: http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html

            BuiltInCategory[] bics = new BuiltInCategory[]
            {
                //BuiltInCategory.OST_CableTray,
                //BuiltInCategory.OST_CableTrayFitting,
                //BuiltInCategory.OST_Conduit,
                //BuiltInCategory.OST_ConduitFitting,
                //BuiltInCategory.OST_DuctCurves,
                //BuiltInCategory.OST_DuctFitting,
                //BuiltInCategory.OST_DuctTerminal,
                //BuiltInCategory.OST_ElectricalEquipment,
                //BuiltInCategory.OST_ElectricalFixtures,
                //BuiltInCategory.OST_LightingDevices,
                //BuiltInCategory.OST_LightingFixtures,
                //BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                //BuiltInCategory.OST_PlumbingFixtures,
                //BuiltInCategory.OST_SpecialityEquipment,
                //BuiltInCategory.OST_Sprinklers,
                //BuiltInCategory.OST_Wire
            };

            IList<ElementFilter> a = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));

            //IList<ElementFilter> b = new List<ElementFilter>(6);
            IList<ElementFilter> b = new List<ElementFilter>
            {

                //b.Add(new ElementClassFilter(typeof(CableTray)));
                //b.Add(new ElementClassFilter(typeof(Conduit)));
                //b.Add(new ElementClassFilter(typeof(Duct)));
                new ElementClassFilter(typeof(Pipe)),

                familyInstanceFilter
            };
            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }
    }

    internal static class FilterDiameterLimit
    {
        /// <summary>
        /// Tests the diameter of the pipe or primary connector of element against the diameter limit set in the interface.
        /// </summary>
        /// <param name="passedElement"></param>
        /// <returns>True if diameter is larger than limit and false if smaller.</returns>
        internal static bool FilterDL(Element element)
        {
            double diameterLimit = iv.DiameterLimit;
            bool diameterLimitBool = true;
            double testedDiameter = 0;
            switch (element.Category.Id.IntegerValue)
            {
                case (int)BuiltInCategory.OST_PipeCurves:
                    if (iv.UNITS_BORE_MM) testedDiameter = double.Parse(Conversion.PipeSizeToMm(((MEPCurve)element).Diameter / 2));
                    else if (iv.UNITS_BORE_INCH) testedDiameter = double.Parse(Conversion.PipeSizeToInch(((MEPCurve)element).Diameter / 2));

                    if (testedDiameter <= diameterLimit) diameterLimitBool = false;

                    break;

                case (int)BuiltInCategory.OST_PipeFitting:
                case (int)BuiltInCategory.OST_PipeAccessory:

                    //Declare a variable for 
                    Connector testedConnector = null;

                    //Gather connectors of the element
                    var cons = MepUtils.GetConnectors(element);

                    if (cons.Primary == null) break;
                    else if (cons.Count == 0) break;
                    else if (cons.Count == 1 || cons.Count > 2) testedConnector = cons.Primary;
                    else if (cons.Count == 2) testedConnector = cons.Largest ?? cons.Primary; //Largest is only defined for reducers

                    if (iv.UNITS_BORE_MM) testedDiameter = (testedConnector.Radius * 2).FtToMm().Round();
                    else if (iv.UNITS_BORE_INCH) testedDiameter = (testedConnector.Radius * 2).FtToInch().Round(3);

                    if (testedDiameter <= diameterLimit) diameterLimitBool = false;

                    break;
            }
            return diameterLimitBool;
        }
    }

    public static class GeometricalComparison
    {
        #region Geometrical Comparison
        //const double _eps = 1.0e-9; //Original tolerance
        const double _eps = 0.00328; //Tolerance equal to 1 mm

        public static double Eps => _eps;

        public static double MinLineLength => _eps;

        public static double TolPointOnPlane => _eps;

        public static bool IsZero(double a, double tolerance) => tolerance > Math.Abs(a);

        public static bool IsZero(double a) => IsZero(a, _eps);

        public static bool IsEqual(double a, double b) => IsZero(b - a);

        public static bool IsEqual(double a, double b, double tolerance) => IsZero(b - a, tolerance);

        public static bool Equalz(this double a, double b, double tolerance) => IsZero(b - a, tolerance);

        public static int Compare(double a, double b) => IsEqual(a, b) ? 0 : (a < b ? -1 : 1);

        public static int Compare(XYZ p, XYZ q)
        {
            int d = Compare(p.X, q.X);
            if (0 == d)
            {
                d = Compare(p.Y, q.Y);
                if (0 == d) d = Compare(p.Z, q.Z);
            }
            return d;
        }

        public static int Compare(Plane a, Plane b)
        {
            int d = Compare(a.Normal, b.Normal);
            if (0 == d)
            {
                d = Compare(a.SignedDistanceTo(XYZ.Zero),
                  b.SignedDistanceTo(XYZ.Zero));
                if (0 == d)
                {
                    d = Compare(a.XVec.AngleOnPlaneTo(
                      b.XVec, b.Normal), 0);
                }
            }
            return d;
        }

        public static bool IsEqual(this XYZ p, XYZ q) => 0 == Compare(p, q);

        public static bool IsEqual(this Connector c1, Connector c2) => c1.Origin.IsEqual(c2.Origin);

        /// <summary>
        /// Return true if the given bounding box bb
        /// contains the given point p in its interior.
        /// </summary>
        public static bool BoundingBoxXyzContains(BoundingBoxXYZ bb, XYZ p) => 0 < Compare(bb.Min, p) && 0 < Compare(p, bb.Max);

        /// <summary>
        /// Return true if the vectors v and w 
        /// are non-zero and perpendicular.
        /// </summary>
        static bool IsPerpendicular(XYZ v, XYZ w)
        {
            double a = v.GetLength();
            double b = v.GetLength();
            double c = Math.Abs(v.DotProduct(w));
            return _eps < a
              && _eps < b
              && _eps > c;
            // c * c < _eps * a * b
        }

        public static bool IsParallel(XYZ p, XYZ q) => p.CrossProduct(q).IsZeroLength();

        public static bool IsHorizontal(XYZ v) => IsZero(v.Z);

        public static bool IsVertical(XYZ v) => IsZero(v.X) && IsZero(v.Y);

        public static bool IsVertical(XYZ v, double tolerance) => IsZero(v.X, tolerance) && IsZero(v.Y, tolerance);

        public static bool IsHorizontal(Edge e)
        {
            XYZ p = e.Evaluate(0);
            XYZ q = e.Evaluate(1);
            return IsHorizontal(q - p);
        }

        public static bool IsHorizontal(PlanarFace f) => IsVertical(f.FaceNormal);

        public static bool IsVertical(PlanarFace f) => IsHorizontal(f.FaceNormal);

        public static bool IsVertical(CylindricalFace f) => IsVertical(f.Axis);

        /// <summary>
        /// Minimum slope for a vector to be considered
        /// to be pointing upwards. Slope is simply the
        /// relationship between the vertical and
        /// horizontal components.
        /// </summary>
        const double _minimumSlope = 0.3;

        /// <summary>
        /// Return true if the Z coordinate of the
        /// given vector is positive and the slope
        /// is larger than the minimum limit.
        /// </summary>
        public static bool PointsUpwards(XYZ v)
        {
            double horizontalLength = v.X * v.X + v.Y * v.Y;
            double verticalLength = v.Z * v.Z;

            return 0 < v.Z
              && _minimumSlope
                < verticalLength / horizontalLength;

            //return _eps < v.Normalize().Z;
            //return _eps < v.Normalize().Z && IsVertical( v.Normalize(), tolerance );
        }

        /// <summary>
        /// Return the maximum value from an array of real numbers.
        /// </summary>
        public static double Max(double[] a)
        {
            Debug.Assert(1 == a.Rank, "expected one-dimensional array");
            Debug.Assert(0 == a.GetLowerBound(0), "expected zero-based array");
            Debug.Assert(0 < a.GetUpperBound(0), "expected non-empty array");
            double max = a[0];
            for (int i = 1; i <= a.GetUpperBound(0); ++i)
            {
                if (max < a[i])
                {
                    max = a[i];
                }
            }
            return max;
        }
        #endregion // Geometrical Comparison
    }

    public static class Conversion
    {
        const double _inch_to_mm = 25.4;
        const double _foot_to_mm = 12 * _inch_to_mm;
        const double _foot_to_inch = 12;

        /// <summary>
        /// Return a string for a real number.
        /// </summary>
        private static string RealString(double a)
        {
            //return a.ToString("0.##");
            //return (Math.Truncate(a * 100) / 100).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"));
            return Math.Round(a, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.GetCultureInfo("en-GB"));
        }

        /// <summary>
        /// Return a string for an XYZ point or vector with its coordinates converted from feet to millimetres.
        /// </summary>
        public static string PointStringMm(XYZ p)
        {
            return string.Format("{0:0} {1:0} {2:0}",
              RealString(p.X * _foot_to_mm),
              RealString(p.Y * _foot_to_mm),
              RealString(p.Z * _foot_to_mm));
        }

        public static string PointStringInch(XYZ p)
        {
            return string.Format("{0:0.00} {1:0.00} {2:0.00}",
              RealString(p.X * _foot_to_inch),
              RealString(p.Y * _foot_to_inch),
              RealString(p.Z * _foot_to_inch));
        }

        public static string PipeSizeToMm(double l)
        {
            return string.Format("{0}", Math.Round(l * 2 * _foot_to_mm));
        }

        public static string PipeSizeToInch(double l)
        {
            return string.Format("{0}", RealString(l * 2 * _foot_to_inch));
        }

        public static string AngleToPCF(double l)
        {
            return string.Format("{0}", l);
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }

    public static class Extensions
    {
        const double _inch_to_mm = 25.4;
        const double _foot_to_mm = 12 * _inch_to_mm;
        const double _foot_to_inch = 12;

        public static double Round2(this Double number)
        {
            return Math.Round(number, 2, MidpointRounding.AwayFromZero);
        }

        public static double Round(this Double number, int decimals = 0)
        {
            return Math.Round(number, decimals, MidpointRounding.AwayFromZero);
        }

        public static double FtToMm(this Double l) => l * _foot_to_mm;

        public static double FtToInch(this Double l) => l * _foot_to_inch;

        public static int NrOfDigits(this string value)
        {
            int index = value.IndexOf('.');
            return index >= 0 ? value.Length - (index + 1) : 0;
        }

        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
        }

        public static IEnumerable<T> ExceptWhere<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            return source.Where(x => !predicate(x));
        }
    }

    public class EndWriter
    {
        public static StringBuilder WriteEP1(Element element, Connector connector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector.Origin;
            double connectorSize = connector.Radius;
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(connectorOrigin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(connectorOrigin));
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_END1").AsString()) == false)
            {
                sbEndWriter.Append(" ");
                sbEndWriter.Append(element.LookupParameter("PCF_ELEM_END1").AsString());
            }
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteEP2(Element element, Connector connector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector.Origin;
            double connectorSize = connector.Radius;
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(connectorOrigin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(connectorOrigin));
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_END2").AsString()) == false)
            {
                sbEndWriter.Append(" ");
                sbEndWriter.Append(element.LookupParameter("PCF_ELEM_END2").AsString());
            }
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteEP2(Element element, XYZ connector, double size)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector;
            double connectorSize = size;
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(connectorOrigin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(connectorOrigin));
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_END2").AsString()) == false)
            {
                sbEndWriter.Append(" ");
                sbEndWriter.Append(element.LookupParameter("PCF_ELEM_END2").AsString());
            }
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteEP3(Element element, Connector connector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector.Origin;
            double connectorSize = connector.Radius;
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(connectorOrigin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(connectorOrigin));
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_END3").AsString()) == false)
            {
                sbEndWriter.Append(" ");
                sbEndWriter.Append(element.LookupParameter("PCF_ELEM_END3").AsString());
            }
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteBP1(Element element, Connector connector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector.Origin;
            double connectorSize = connector.Radius;
            sbEndWriter.Append("    BRANCH1-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(connectorOrigin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(connectorOrigin));
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_BP1").AsString()) == false)
            {
                sbEndWriter.Append(" ");
                sbEndWriter.Append(element.LookupParameter("PCF_ELEM_BP1").AsString());
            }
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCP(FamilyInstance familyInstance)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ elementLocation = ((LocationPoint)familyInstance.Location).Point;
            sbEndWriter.Append("    CENTRE-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(elementLocation));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(elementLocation));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCP(XYZ point)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    CENTRE-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(point));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(point));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(XYZ point)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(point));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(point));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(FamilyInstance familyInstance, Connector passedConnector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ elementLocation = ((LocationPoint)familyInstance.Location).Point;
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(Conversion.PointStringMm(elementLocation));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(elementLocation));
            double connectorSize = passedConnector.Radius;
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

    }

    public class ScheduleCreator
    {
        //private UIDocument _uiDoc;
        public Result CreateAllItemsSchedule(UIDocument uiDoc)
        {
            try
            {
                Document doc = uiDoc.Document;
                FilteredElementCollector sharedParameters = new FilteredElementCollector(doc);
                sharedParameters.OfClass(typeof(SharedParameterElement));

                #region Debug

                ////Debug
                //StringBuilder sbDev = new StringBuilder();
                //var list = new ParameterDefinition().ElementParametersAll;
                //int i = 0;

                //foreach (SharedParameterElement sp in sharedParameters)
                //{
                //    sbDev.Append(sp.GuidValue + "\n");
                //    sbDev.Append(list[i].Guid.ToString() + "\n");
                //    i++;
                //    if (i == list.Count) break;
                //}
                ////sbDev.Append( + "\n");
                //// Clear the output file
                //File.WriteAllBytes(InputVars.OutputDirectoryFilePath + "\\Dev.pcf", new byte[0]);

                //// Write to output file
                //using (StreamWriter w = File.AppendText(InputVars.OutputDirectoryFilePath + "\\Dev.pcf"))
                //{
                //    w.Write(sbDev);
                //    w.Close();
                //}

                #endregion

                Transaction t = new Transaction(doc, "Create items schedules");
                t.Start();

                #region Schedule ALL elements
                ViewSchedule schedAll = ViewSchedule.CreateSchedule(doc, ElementId.InvalidElementId,
                    ElementId.InvalidElementId);
                schedAll.Name = "PCF - ALL Elements";
                schedAll.Definition.IsItemized = false;

                IList<SchedulableField> schFields = schedAll.Definition.GetSchedulableFields();

                foreach (SchedulableField schField in schFields)
                {
                    if (schField.GetName(doc) != "Family and Type") continue;
                    ScheduleField field = schedAll.Definition.AddField(schField);
                    ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(field.FieldId);
                    schedAll.Definition.AddSortGroupField(sortGroupField);
                }

                string curUsage = "U";
                string curDomain = "ELEM";
                var query = from p in new plst().LPAll where p.Usage == curUsage && p.Domain == curDomain select p;

                foreach (pdef pDef in query.ToList())
                {
                    SharedParameterElement parameter = (from SharedParameterElement param in sharedParameters
                                                        where param.GuidValue.CompareTo(pDef.Guid) == 0
                                                        select param).First();
                    SchedulableField queryField = (from fld in schFields where fld.ParameterId.IntegerValue == parameter.Id.IntegerValue select fld).First();

                    ScheduleField field = schedAll.Definition.AddField(queryField);
                    if (pDef.Name != "PCF_ELEM_TYPE") continue;
                    ScheduleFilter filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.HasParameter);
                    schedAll.Definition.AddFilter(filter);
                }
                #endregion

                #region Schedule FILTERED elements
                ViewSchedule schedFilter = ViewSchedule.CreateSchedule(doc, ElementId.InvalidElementId,
                    ElementId.InvalidElementId);
                schedFilter.Name = "PCF - Filtered Elements";
                schedFilter.Definition.IsItemized = false;

                schFields = schedFilter.Definition.GetSchedulableFields();

                foreach (SchedulableField schField in schFields)
                {
                    if (schField.GetName(doc) != "Family and Type") continue;
                    ScheduleField field = schedFilter.Definition.AddField(schField);
                    ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(field.FieldId);
                    schedFilter.Definition.AddSortGroupField(sortGroupField);
                }

                foreach (pdef pDef in query.ToList())
                {
                    SharedParameterElement parameter = (from SharedParameterElement param in sharedParameters
                                                        where param.GuidValue.CompareTo(pDef.Guid) == 0
                                                        select param).First();
                    SchedulableField queryField = (from fld in schFields where fld.ParameterId.IntegerValue == parameter.Id.IntegerValue select fld).First();

                    ScheduleField field = schedFilter.Definition.AddField(queryField);
                    if (pDef.Name != "PCF_ELEM_TYPE") continue;
                    ScheduleFilter filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.HasParameter);
                    schedFilter.Definition.AddFilter(filter);
                    filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.NotEqual, "");
                    schedFilter.Definition.AddFilter(filter);
                }
                #endregion

                #region Schedule Pipelines
                ViewSchedule schedPipeline = ViewSchedule.CreateSchedule(doc, new ElementId(BuiltInCategory.OST_PipingSystem), ElementId.InvalidElementId);
                schedPipeline.Name = "PCF - Pipelines";
                schedPipeline.Definition.IsItemized = false;

                schFields = schedPipeline.Definition.GetSchedulableFields();

                foreach (SchedulableField schField in schFields)
                {
                    if (schField.GetName(doc) != "Family and Type") continue;
                    ScheduleField field = schedPipeline.Definition.AddField(schField);
                    ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(field.FieldId);
                    schedPipeline.Definition.AddSortGroupField(sortGroupField);
                }

                curDomain = "PIPL";
                foreach (pdef pDef in query.ToList())
                {
                    SharedParameterElement parameter = (from SharedParameterElement param in sharedParameters
                                                        where param.GuidValue.CompareTo(pDef.Guid) == 0
                                                        select param).First();
                    SchedulableField queryField = (from fld in schFields where fld.ParameterId.IntegerValue == parameter.Id.IntegerValue select fld).First();
                    schedPipeline.Definition.AddField(queryField);
                }
                #endregion

                t.Commit();

                sharedParameters.Dispose();

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Util.InfoMsg(e.Message);
                return Result.Failed;
            }



        }
    }

    public class DataHandler
    {
        //DataSet import is from here:
        //http://stackoverflow.com/a/18006593/6073998
        public static DataSet ImportExcelToDataSet(string fileName, string dataHasHeaders)
        {
            //On connection strings http://www.connectionstrings.com/excel/#p84
            string connectionString =
                string.Format(
                    "provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0;HDR={1};IMEX=1\"",
                    fileName, dataHasHeaders);

            DataSet data = new DataSet();

            foreach (string sheetName in GetExcelSheetNames(connectionString))
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    var dataTable = new DataTable();
                    string query = string.Format("SELECT * FROM [{0}]", sheetName);
                    con.Open();
                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, con);
                    adapter.Fill(dataTable);

                    //Remove ' and $ from sheetName
                    Regex rgx = new Regex("[^a-zA-Z0-9 _-]");
                    string tableName = rgx.Replace(sheetName, "");

                    dataTable.TableName = tableName;
                    data.Tables.Add(dataTable);
                }
            }

            if (data == null) Util.ErrorMsg("Data set is null");
            if (data.Tables.Count < 1) Util.ErrorMsg("Table count in DataSet is 0");

            return data;
        }

        static string[] GetExcelSheetNames(string connectionString)
        {
            OleDbConnection con = null;
            DataTable dt = null;
            con = new OleDbConnection(connectionString);
            con.Open();
            dt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            if (dt == null)
            {
                return null;
            }

            string[] excelSheetNames = new string[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                excelSheetNames[i] = row["TABLE_NAME"].ToString();
                i++;
            }

            return excelSheetNames;
        }
    }

    public static class MepUtils
    {
        /// <summary>
        /// Return the given element's connector manager, 
        /// using either the family instance MEPModel or 
        /// directly from the MEPCurve connector manager
        /// for ducts and pipes.
        /// </summary>
        public static ConnectorManager GetConnectorManager(Element e)
        {
            MEPCurve mc = e as MEPCurve;
            FamilyInstance fi = e as FamilyInstance;

            if (null == mc && null == fi)
            {
                throw new ArgumentException(
                  "Element is neither an MEP curve nor a fitting.");
            }

            return null == mc
              ? fi.MEPModel.ConnectorManager
              : mc.ConnectorManager;
        }

        public static PipingSystemType GetElementPipingSystemType(Element element, Document doc)
        {
            //Retrieve Element PipingSystemType
            ElementId sysTypeId;

            if (element is MEPCurve pipe)
            {
                sysTypeId = pipe.MEPSystem.GetTypeId();
            }
            else if (element is FamilyInstance famInst)
            {
                ConnectorSet cSet = famInst.MEPModel.ConnectorManager.Connectors;
                Connector con =
                    (from Connector c in cSet where c.GetMEPConnectorInfo().IsPrimary select c).FirstOrDefault();
                sysTypeId = con.MEPSystem.GetTypeId();
            }
            else throw new Exception("Trying to get PipingSystemType from nor MEPCurve nor FamilyInstance for element: " + element.Id.IntegerValue);

            return (PipingSystemType)doc.GetElement(sysTypeId);
        }

        public static IList<string> GetDistinctPhysicalPipingSystemTypeNames(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            HashSet<PipingSystem> pipingSystems = collector.OfClass(typeof(PipingSystem)).Cast<PipingSystem>().ToHashSet();
            HashSet<PipingSystemType> pipingSystemTypes = pipingSystems.Select(ps => doc.GetElement(ps.GetTypeId())).Cast<PipingSystemType>().ToHashSet();

            //Following code takes care if PCF_PIPL_EXCL has not been properly imported.
            PipingSystemType pstype = pipingSystemTypes.FirstOrDefault();
            if (pstype == null) throw new Exception("No piping systems created yet! Draw some pipes.");

            HashSet<string> abbreviations;
            if (pstype.get_Parameter(new plst().CII_PIPL_EXCL.Guid) == null)
            {
                abbreviations = pipingSystemTypes.Select(pst => pst.Abbreviation).ToHashSet();
            }
            else
            {
                abbreviations = pipingSystemTypes
                      .Where(pst => pst.get_Parameter(new plst().CII_PIPL_EXCL.Guid).AsInteger() == 0) //Filter out EXCLUDED piping systems
                      .Select(pst => pst.Abbreviation).ToHashSet();
            }

            return abbreviations.Distinct().ToList();
        }

        public static Cons GetConnectors(Element element) => new Cons(element);

        public static FilteredElementCollector GetElementsWithConnectors(Document doc)
        {
            // what categories of family instances
            // are we interested in?
            // From here: http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html

            BuiltInCategory[] bics = new BuiltInCategory[]
            {
                //BuiltInCategory.OST_CableTray,
                //BuiltInCategory.OST_CableTrayFitting,
                //BuiltInCategory.OST_Conduit,
                //BuiltInCategory.OST_ConduitFitting,
                //BuiltInCategory.OST_DuctCurves,
                //BuiltInCategory.OST_DuctFitting,
                //BuiltInCategory.OST_DuctTerminal,
                //BuiltInCategory.OST_ElectricalEquipment,
                //BuiltInCategory.OST_ElectricalFixtures,
                //BuiltInCategory.OST_LightingDevices,
                //BuiltInCategory.OST_LightingFixtures,
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                //BuiltInCategory.OST_PlumbingFixtures,
                //BuiltInCategory.OST_SpecialityEquipment,
                //BuiltInCategory.OST_Sprinklers,
                //BuiltInCategory.OST_Wire
            };

            IList<ElementFilter> a = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));

            //IList<ElementFilter> b = new List<ElementFilter>(6);
            IList<ElementFilter> b = new List<ElementFilter>
            {

                //b.Add(new ElementClassFilter(typeof(CableTray)));
                //b.Add(new ElementClassFilter(typeof(Conduit)));
                //b.Add(new ElementClassFilter(typeof(Duct)));
                new ElementClassFilter(typeof(Pipe)),

                familyInstanceFilter
            };
            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }

        /// <summary>
        /// Returns the collection of all instances of the specified BuiltInCategory. Warning: does not work with Pipes!
        /// </summary>
        /// <param name="doc">The active document.</param>
        /// <param name="cat">The specified category. Ex: BuiltInCategory.OST_PipeFitting</param>
        /// <returns>A collection of all instances of the specified category.</returns>
        public static FilteredElementCollector GetElementsOfBuiltInCategory(Document doc, BuiltInCategory cat)
        {
            // what categories of family instances
            // are we interested in?
            // From here: http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html

            ElementFilter categoryFilter = new ElementCategoryFilter(cat);
            LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.WherePasses(familyInstanceFilter);
            return collector;
        }

        public static ConnectorSet GetConnectorSet(Element e)
        {
            ConnectorSet connectors = null;

            if (e is FamilyInstance)
            {
                MEPModel m = ((FamilyInstance)e).MEPModel;
                if (null != m && null != m.ConnectorManager) connectors = m.ConnectorManager.Connectors;
            }

            else if (e is Wire) connectors = ((Wire)e).ConnectorManager.Connectors;

            else
            {
                Debug.Assert(e.GetType().IsSubclassOf(typeof(MEPCurve)),
                    "expected all candidate connector provider "
                    + "elements to be either family instances or "
                    + "derived from MEPCurve");

                if (e is MEPCurve) connectors = ((MEPCurve)e).ConnectorManager.Connectors;
            }
            return connectors;
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(HashSet<Element> elements)
        {
            return (from e in elements from Connector c in GetConnectorSet(e) select c).ToHashSet();
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(Element element)
        {
            return (from Connector c in GetConnectorSet(element) select c).ToHashSet();
        }

        public static HashSet<Connector> GetALLConnectorsInDocument(Document doc)
        {
            return (from e in GetElementsWithConnectors(doc) from Connector c in GetConnectorSet(e) select c).ToHashSet();
        }

        /// <summary>
        /// Compare Connector objects based on their location point.
        /// </summary>
        public class ConnectorXyzComparer : IEqualityComparer<Connector>
        {
            public bool Equals(Connector x, Connector y)
            {
                return null != x && null != y && GeometricalComparison.IsEqual(x.Origin, y.Origin);
            }

            public int GetHashCode(Connector x)
            {
                return HashString(x.Origin).GetHashCode();
            }
        }

        /// <summary>
        /// Return a hash string for a real number
        /// formatted to nine decimal places.
        /// </summary>
        public static string HashString(double a)
        {
            return a.ToString("0.#########");
        }

        /// <summary>
        /// Return a hash string for an XYZ point
        /// or vector with its coordinates
        /// formatted to nine decimal places.
        /// </summary>
        public static string HashString(XYZ p)
        {
            return string.Format("({0},{1},{2})",
              HashString(p.X),
              HashString(p.Y),
              HashString(p.Z));
        }

        internal static bool IsTheElementACap(Element element)
        {
            switch (element)
            {
                case Pipe pipe:
                    return false;
                case FamilyInstance fi:
                    int cat = fi.Category.Id.IntegerValue;
                    switch (cat)
                    {
                        case (int)BuiltInCategory.OST_PipeFitting:
                            var mf = fi.MEPModel as MechanicalFitting;
                            var partType = mf.PartType;
                            switch (partType)
                            {
                                case PartType.Cap:
                                    //Flanges share the same PartType with Caps
                                    //I assume that Family Name of the Cap contains word "Cap"
                                    //Thus filtering for Cap families and retaining flanges
                                    Parameter para = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                                    string familyName = para.AsValueString();
                                    if (familyName.Contains("Cap")) return true;
                                    else return false;
                                default:
                                    return false;
                            }
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        internal static Dictionary<int, double> pipeWallThkDict()
        {
            return new Dictionary<int, double>
            {
                [10] = 1.8,
                [15] = 2.0,
                [20] = 2.3,
                [25] = 2.6,
                [32] = 2.6,
                [40] = 2.6,
                [50] = 2.9,
                [65] = 2.9,
                [80] = 3.2,
                [100] = 3.6,
                [125] = 4.0,
                [150] = 4.5,
                [200] = 6.3,
                [250] = 6.3,
                [300] = 7.1,
                [350] = 8.0,
                [400] = 8.8,
                [450] = 10.0,
                [500] = 11.0,
                [600] = 12.5
            };
        }

        internal static Dictionary<int, double> outerDiaDict()
        {
            return new Dictionary<int, double>
            {
                [10] = 17.2,
                [15] = 21.3,
                [20] = 26.9,
                [25] = 33.7,
                [32] = 42.4,
                [40] = 48.3,
                [50] = 60.3,
                [65] = 76.1,
                [80] = 88.9,
                [100] = 114.3,
                [125] = 139.7,
                [150] = 168.3,
                [200] = 219.1,
                [250] = 273.0,
                [300] = 323.9,
                [350] = 355.6,
                [400] = 406.4,
                [450] = 457.0,
                [500] = 508.0,
                [600] = 610.0
            };
        }
    }

    public class Cons
    {
        public Connector Primary { get; } = null;
        public Connector Secondary { get; } = null;
        public Connector Tertiary { get; } = null;
        public int Count { get; } = 0;
        public Connector Largest { get; } = null;
        public Connector Smallest { get; } = null;

        public Cons(Element element)
        {
            ConnectorManager cmgr = MepUtils.GetConnectorManager(element);

            foreach (Connector connector in cmgr.Connectors)
            {
                Count++;
                if (connector.GetMEPConnectorInfo().IsPrimary) Primary = connector;
                else if (connector.GetMEPConnectorInfo().IsSecondary) Secondary = connector;
                else if ((connector.GetMEPConnectorInfo().IsPrimary == false) && (connector.GetMEPConnectorInfo().IsSecondary == false))
                    Tertiary = connector;
            }

            if (Count > 1 && Secondary == null)
                throw new Exception($"Element {element.Id.ToString()} has {Count} connectors and no secondary!");

            if (element is FamilyInstance)
            {
                if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                {
                    var mf = ((FamilyInstance)element).MEPModel as MechanicalFitting;

                    if (mf.PartType.ToString() == "Transition")
                    {
                        double primDia = (Primary.Radius * 2).Round(3);
                        double secDia = (Secondary.Radius * 2).Round(3);

                        Largest = primDia > secDia ? Primary : Secondary;
                        Smallest = primDia < secDia ? Primary : Secondary;
                    }
                }
            }
        }
    }
}