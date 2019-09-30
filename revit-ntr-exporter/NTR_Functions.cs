using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Shared;
using MoreLinq;
using iv = NTR_Functions.InputVars;
using xel = Microsoft.Office.Interop.Excel;
using Autodesk.Revit.DB.Mechanical;

namespace NTR_Functions
{
    public static class InputVars
    {
        //Scope control
        public static bool ExportAllOneFile = false;
        public static bool ExportAllSepFiles = false;
        public static bool ExportSpecificPipeLine = false;
        public static bool ExportSelection = false;
        public static double DiameterLimitGreaterOrEqThan = 0;
        public static double DiameterLimitLessOrEqThan = 9999;

        //File control
        public static string OutputDirectoryFilePath = @"C:\";
        public static string ExcelPath = @"C:\";

        //Current SystemAbbreviation
        public static string SysAbbr = "FVF";
        public static BuiltInParameter SysAbbrParam = BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM;
    }

    public class ConfigurationData
    {
        public StringBuilder _01_GEN { get; }
        public StringBuilder _02_AUFT { get; }
        public StringBuilder _03_TEXT { get; }
        public StringBuilder _04_LAST { get; }
        public StringBuilder _05_DN { get; }
        public StringBuilder _06_ISO { get; }
        public DataTable Pipelines { get; }
        public DataTable Elements { get; }
        public DataTable Supports { get; }
        public DataTable Profiles { get; }
        public DataTable Flexjoints { get; }

        public ConfigurationData()
        {
            DataSet dataSet = Shared.DataHandler.ImportExcelToDataSet(iv.ExcelPath, "NO");

            DataTableCollection dataTableCollection = dataSet.Tables;

            _01_GEN = ReadNtrConfigurationData(dataTableCollection, "GEN", "C General settings");
            _02_AUFT = ReadNtrConfigurationData(dataTableCollection, "AUFT", "C Project description");
            _03_TEXT = ReadNtrConfigurationData(dataTableCollection, "TEXT", "C User text");
            _04_LAST = ReadNtrConfigurationData(dataTableCollection, "LAST", "C Loads definition");
            _05_DN = ReadNtrConfigurationData(dataTableCollection, "DN", "C Definition of pipe dimensions");
            _06_ISO = ReadNtrConfigurationData(dataTableCollection, "IS", "C Definition of insulation type");

            DataSet dataSetWithHeaders = Shared.DataHandler.ImportExcelToDataSet(iv.ExcelPath, "YES");
            Pipelines = ReadDataTable(dataSetWithHeaders.Tables, "PIPELINES");
            Elements = ReadDataTable(dataSetWithHeaders.Tables, "ELEMENTS");
            Supports = ReadDataTable(dataSetWithHeaders.Tables, "SUPPORTS");
            Profiles = ReadDataTable(dataSetWithHeaders.Tables, "PROFILES");
            Flexjoints = ReadDataTable(dataSetWithHeaders.Tables, "FLEXJOINTS");

            //http://stackoverflow.com/questions/10855/linq-query-on-a-datatable?rq=1
        }

        /// <summary>
        /// Selects a DataTable by name and creates a StringBuilder output to NTR format based on the data in table.
        /// </summary>
        /// <param name="dataTableCollection">A collection of datatables.</param>
        /// <param name="tableName">The name of the DataTable to process.</param>
        /// <returns>StringBuilder containing the output NTR data.</returns>
        private static StringBuilder ReadNtrConfigurationData(DataTableCollection dataTableCollection, string tableName, string description)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(description);

            var table = ReadDataTable(dataTableCollection, tableName);
            if (table == null)
            {
                sb.AppendLine("C " + tableName + " does not exist!");
                return sb;
            }

            int numberOfRows = table.Rows.Count;
            if (numberOfRows.IsOdd())
            {
                sb.AppendLine("C " + tableName + " is malformed, contains odd number of rows, must be even");
                return sb;
            }

            for (int i = 0; i < numberOfRows / 2; i++)
            {
                DataRow headerRow = table.Rows[i * 2];
                DataRow dataRow = table.Rows[i * 2 + 1];
                if (headerRow == null || dataRow == null)
                    throw new NullReferenceException(
                        tableName + " does not have two rows, check EXCEL configuration sheet!");

                sb.Append(tableName);

                for (int j = 0; j < headerRow.ItemArray.Length; j++)
                {
                    sb.Append(" ");
                    sb.Append(headerRow.Field<string>(j));
                    sb.Append("=");
                    sb.Append(dataRow.Field<string>(j));
                }

                sb.AppendLine();
            }

            return sb;
        }

        public static DataTable ReadDataTable(DataTableCollection dataTableCollection, string tableName)
        {
            return (from DataTable dtbl in dataTableCollection where dtbl.TableName == tableName select dtbl).FirstOrDefault();
        }
    }

    public static class DataWriter
    {
        public static string PointCoords<T>(string p, T subj)
        {
            switch (subj)
            {
                case Connector c:
                    return " " + p + "=" + NtrConversion.PointStringMm(c.Origin);
                case Element element:
                    return " " + p + "=" + NtrConversion.PointStringMm(((LocationPoint)element.Location).Point);
                case XYZ point:
                    return " " + p + "=" + NtrConversion.PointStringMm(point);
                default:
                    return "PointCoords in DataWriter does not handle this type of data!";
            }
        }

        public static string PointCoordsHanger(string p, Element element)
        {
            XYZ location = ((LocationPoint)element.Location).Point;

            Parameter offsetFromLvlPar = element.LookupParameter("PipeOffsetFromLevel");
            double PipeOffsetFromLevel = offsetFromLvlPar.AsDouble();
            XYZ modLocation = new XYZ(location.X, location.Y, location.Z + PipeOffsetFromLevel);

            return " " + p + "=" + NtrConversion.PointStringMm(modLocation);
        }

        internal static string HangerLength(string p, Element element)
        {
            string valueString = element.LookupParameter("HangerLength").AsValueString();
            double value = double.Parse(valueString).Round() / 1000;

            return (" " + p + "=" + value).Replace(",", ".");
        }

        internal static string ParameterValue(string keyword, string parName, Element element)
        {
            string valueString = element.LookupParameter(parName).ToValueString();
            if (valueString.IsNullOrEmpty()) return "";
            return $" {keyword}={valueString}";
        }

        internal static string ParameterValue(string keyword, string[] parNames, Element element)
        {
            List<string> values = new List<string>();
            foreach (string name in parNames)
            {
                string value = element.LookupParameter(name).ToValueString();
                if (!value.IsNullOrEmpty()) values.Add(value);
            }
            if (values.Count < 1) return "";
            else return $" {keyword}={string.Join("_", values)}";
        }

        internal static string ParameterValue(string keyword, BuiltInParameter bip, Element element)
        {
            Parameter par = element.get_Parameter(bip);
            string valueString = ToValueString(par);
            if (valueString.IsNullOrEmpty()) return "";
            return $" {keyword}={valueString}";
        }

        /// <summary>
        /// Method is taken from here:
        /// https://spiderinnet.typepad.com/blog/2011/08/revit-parameter-api-asvaluestring-tostring-tovaluestring-and-tovaluedisplaystring.html
        /// </summary>
        /// <param name="p">Revit parameter</param>
        /// <returns>Stringified contents of the parameter</returns>
        internal static string ToValueString(this Autodesk.Revit.DB.Parameter p)
        {
            string ret = string.Empty;

            switch (p.StorageType)
            {
                case StorageType.ElementId:
                    ret = p.AsElementId().ToString();
                    break;
                case StorageType.Integer:
                    ret = p.AsInteger().ToString();
                    break;
                case StorageType.String:
                    ret = p.AsString();
                    break;
                case StorageType.Double:
                    ret = p.AsValueString();
                    break;
                default:
                    break;
            }

            return ret;
        }

        public static string DnWriter(Element element)
        {
            double dia = 0;

            if (element is Pipe pipe)
            {
                //Get connector set for the pipes
                ConnectorSet connectorSet = pipe.ConnectorManager.Connectors;
                //Filter out non-end types of connectors
                Connector con = (from Connector connector in connectorSet
                                 where connector.ConnectorType.ToString().Equals("End")
                                 select connector).FirstOrDefault();
                dia = con.Radius * 2;
            }
            else if (element is FamilyInstance fis)
            {
                //TODO: Fix FamilyInstance case, maybe not
                return "NTR_Functions DataWriter DnWriter FamilyInstance case";
            }
            return " DN=DN" + dia.FtToMm().Round(0);
        }

        public static string DnWriter(string p, Connector con)
        {
            double dia = con.Radius * 2;
            return " " + p + "=DN" + dia.FtToMm().Round(0);
        }

        public static string ReadPropertyFromDataTable(string key, DataTable table, string parameter)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(0) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(0) == key
                            select row.Field<string>(parameter);
                string value = query.FirstOrDefault();
                if (string.IsNullOrEmpty(value)) return "";
                return " " + parameter + "=" + value;
            }
            return "";
        }

        public static string ReadPropertyValueFromDataTable(string key, DataTable table, string parameter)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(0) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(0) == key
                            select row.Field<string>(parameter);
                string value = query.FirstOrDefault();
                if (string.IsNullOrEmpty(value)) return "";
                return value;
            }
            return "";
        }

        public static string ReadElementTypeFromDataTable(string key, DataTable table, string parameter)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(0) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(0) == key
                            select row.Field<string>(parameter);
                string value = query.FirstOrDefault();
                if (value == null)
                    throw new Exception("There was no definition for " + parameter + " parameter for pipeline " + key);
                return value;
            }
            return "";
        }

        public static string WriteElementId(Element element, string parameter)
        {
            return " " + parameter + "=" + element.Id.IntegerValue;
        }

        internal static XYZ OletP1Point(Cons cons)
        {
            XYZ endPointOriginOletPrimary = cons.Primary.Origin;
            XYZ endPointOriginOletSecondary = cons.Secondary.Origin;

            //get reference elements
            var refCons = MepUtils.GetAllConnectorsFromConnectorSet(cons.Primary.AllRefs);

            Connector refCon = refCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            if (refCon == null) throw new Exception("refCon Owner cannot find a Pipe for element!");
            Pipe refPipe = (Pipe)refCon.Owner;

            Cons refPipeCons = Shared.MepUtils.GetConnectors(refPipe);

            //Following code is ported from my python solution in Dynamo.
            //The olet geometry is analyzed with congruent rectangles to find the connection point on the pipe even for angled olets.
            XYZ B = endPointOriginOletPrimary; XYZ D = endPointOriginOletSecondary; XYZ pipeEnd1 = refPipeCons.Primary.Origin; XYZ pipeEnd2 = refPipeCons.Secondary.Origin;
            XYZ BDvector = D - B; XYZ ABvector = pipeEnd1 - pipeEnd2;
            double angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            if (angle > 90)
            {
                ABvector = -ABvector;
                angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            }
            Line refsLine = Line.CreateBound(pipeEnd1, pipeEnd2);
            XYZ C = refsLine.Project(B).XYZPoint;
            double L3 = B.DistanceTo(C);
            XYZ E = refsLine.Project(D).XYZPoint;
            double L4 = D.DistanceTo(E);
            double ratio = L4 / L3;
            double L1 = E.DistanceTo(C);
            double L5 = L1 / (ratio - 1);
            XYZ A;
            if (angle < 89)
            {
                XYZ ECvector = C - E;
                ECvector = ECvector.Normalize();
                double L = L1 + L5;
                ECvector = ECvector.Multiply(L);
                A = E.Add(ECvector);
            }
            else A = E;
            return A;
        }
    }

    public class NtrConversion
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
            return Math.Round(a, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.GetCultureInfo("en-GB"));
        }

        /// <summary>
        /// Return a string for an XYZ point or vector with its coordinates converted from feet to millimetres.
        /// </summary>
        public static string PointStringMm(XYZ p)
        {
            return string.Format("'{0:0.0}, {1:0.0}, {2:0.0}'",
                RealString(p.X * _foot_to_mm),
                RealString(p.Y * _foot_to_mm),
                RealString(p.Z * _foot_to_mm));
        }
    }

    public static class NTR_Filter
    {
        /// <summary>
        /// Tests the diameter of the pipe or primary connector of element against the diameter limit set in the interface.
        /// </summary>
        /// <param name="passedElement"></param>
        /// <returns>True if diameter is larger than limit and false if smaller.</returns>
        public static bool FilterDiameterLimit(Element element)
        {
            double diameterMustBeGreater = iv.DiameterLimitGreaterOrEqThan;
            double diameterMustBeLess = iv.DiameterLimitLessOrEqThan;
            double testedDiameter = 0;
            switch (element)
            {
                case MEPCurve pipe:
                    testedDiameter = pipe.Diameter.FtToMm().Round();
                    break;

                case FamilyInstance inst:
                    Cons cons = Shared.MepUtils.GetConnectors(inst);
                    Connector testedConnector = cons.Primary;
                    if (testedConnector == null)
                        throw new Exception("Element " + inst.Id.IntegerValue + " does not have a primary connector!");
                    testedDiameter = (testedConnector.Radius * 2).FtToMm().Round(0);
                    break;
            }

            return testedDiameter >= diameterMustBeGreater && testedDiameter <= diameterMustBeLess;
        }

        public static bool OfPartType(this Element e, PartType pt)
        {
            FamilyInstance fi = (FamilyInstance)e;
            var mf = fi.MEPModel as MechanicalFitting;
            var partType = mf.PartType;
            return 0 == partType.CompareTo(pt);
        }

        public static bool OfConType(this Connector c, ConnectorType ct) => 0 == c.ConnectorType.CompareTo(ct);
        
        public static int OletRefOwnerIdAsInt(this Element e)
        {
            var cons = MepUtils.GetConnectors(e);
            var refCons = MepUtils.GetAllConnectorsFromConnectorSet(cons.Primary.AllRefs);
            Connector refCon = refCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            if (refCon == null) throw new Exception("Olet id " + e.Id.ToString() + " has no reference connector! Check connectivity.");
            Element owner = refCon.Owner;
            return owner.Id.IntegerValue;
        }
    }

    public class NTR_Excel
    {
        public void ExportUndefinedElements(Document doc)
        {
            //Instantiate excel
            xel.Application excel = new xel.Application();
            if (null == excel) throw new Exception("Failed to start EXCEL!");
            excel.Visible = true;
            xel.Workbook workbook = excel.Workbooks.Add(Missing.Value);
            xel.Worksheet worksheet;
            worksheet = excel.ActiveSheet as xel.Worksheet;
            worksheet.Name = "MISSING_ELEMENTS";
            worksheet.Columns.ColumnWidth = 20;

            //Collect all elements
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector = Shared.Filter.GetElementsWithConnectors(doc);
            HashSet<Element> elements = collector.ToElements().ToHashSet();
            HashSet<Element> limitedElements = (from Element e in elements
                                                where NTR_Filter.FilterDiameterLimit(e)
                                                select e).ToHashSet();
            HashSet<Element> filteredElements = (from Element e in limitedElements
                                                 where e.Category.Id.IntegerValue != (int)BuiltInCategory.OST_PipeCurves
                                                 select e).ToHashSet();

            //IOrderedEnumerable<Element> orderedCollector = collector
            //    .OrderBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM)
            //    .AsValueString());

            //IEnumerable<IGrouping<string, Element>> elementGroups =
            //    from e in filteredElements
            //    group e by e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM)
            //    .AsValueString();

            var elGr = filteredElements.GroupBy(x => x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()).OrderBy(x => x.Key);

            //Read existing values
            DataSet dataSetWithHeaders = Shared.DataHandler.ImportExcelToDataSet(iv.ExcelPath, "YES");
            DataTable Elements = ConfigurationData.ReadDataTable(dataSetWithHeaders.Tables, "ELEMENTS");
            DataTable Supports = ConfigurationData.ReadDataTable(dataSetWithHeaders.Tables, "SUPPORTS");
            DataTable Pipelines = ConfigurationData.ReadDataTable(dataSetWithHeaders.Tables, "PIPELINES");

            //Compare values and write those who are not in configuration workbook
            int row = 1;
            int col = 1;
            foreach (IGrouping<string, Element> gp in elGr)
            {
                //See if record already is defined
                if (Elements.AsEnumerable().Any(dataRow => dataRow.Field<string>(0) == gp.Key)) continue;
                if (Supports.AsEnumerable().Any(dataRow => dataRow.Field<string>(0) == gp.Key)) continue;
                worksheet.Cells[row, col] = gp.Key;
                row++;
            }

            //Validate if configuration has all pipelines defined
            //Else no LAST value will be written!
            List<string> pipeSysAbbrs = Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(doc)
                                                       .OrderBy(x => x).ToList();
            foreach (string sa in pipeSysAbbrs)
            {
                string returnValue = DataWriter.ReadPropertyFromDataTable(sa, Pipelines, "LAST");
                if (returnValue.IsNullOrEmpty())
                {
                    worksheet.Cells[row, col] = $"Pipeline: {sa}";
                    row++;
                }
            }

        }
    }
}
