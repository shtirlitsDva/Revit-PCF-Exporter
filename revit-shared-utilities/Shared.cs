using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using Shared.BuildingCoder;

namespace Shared
{
    public static class Filter
    {
        /// <summary>
        /// Generic Parameter value filter. An attempt to write a generic method,
        /// that returns an element filter consumed by FilteredElementCollector.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter VALUE to filter by.</typeparam>
        /// <typeparam name="T2">Type of the PARAMETER to filter.</typeparam>
        /// <param name="value">Currently: string, bool.</param>
        /// <param name="parameterId">Currently: Guid, BuiltInCategory.</param>
        /// <returns>ElementParameterFilter consumed by FilteredElementCollector.</returns>
        public static ElementParameterFilter ParameterValueGenericFilter<T1, T2>(Document doc, T1 value, T2 parameterId)
        {
            //Initialize ParameterValueProvider
            ParameterValueProvider pvp = null;
            switch (parameterId)
            {
                case BuiltInParameter bip:
                    pvp = new ParameterValueProvider(new ElementId((int)bip));
                    break;
                case Guid guid:
                    SharedParameterElement spe = SharedParameterElement.Lookup(doc, guid);
                    pvp = new ParameterValueProvider(spe.Id);
                    break;
                default:
                    throw new NotImplementedException("ParameterValueGenericFilter: T2 (parameter) type not implemented!");
            }

            //Branch off to value types
            switch (value)
            {
                case string str:
                    FilterStringRuleEvaluator fsrE = new FilterStringEquals();
                    FilterStringRule fsr = new FilterStringRule(pvp, fsrE, str, false);
                    return new ElementParameterFilter(fsr);
                case bool bol:
                    int _value;

                    if (bol == true) _value = 1;
                    else _value = 0;

                    FilterNumericRuleEvaluator fnrE = new FilterNumericEquals();
                    FilterIntegerRule fir = new FilterIntegerRule(pvp, fnrE, _value);
                    return new ElementParameterFilter(fir);
                default:
                    throw new NotImplementedException("ParameterValueGenericFilter: T1 (value) type not implemented!");
            }
        }

        public static FilteredElementCollector GetElementsWithConnectors(Document doc, bool includeMechEquipment = false)
        {
            // what categories of family instances
            // are we interested in?
            // From here: http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html

            IList<BuiltInCategory> bics = new List<BuiltInCategory>(3)
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

            if (includeMechEquipment) bics.Add(BuiltInCategory.OST_MechanicalEquipment);

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
        /// Get the collection of elements of the specified type additionally filtered by a string value of specified BuiltInParameter.
        /// </summary>
        /// <typeparam name="T1">The type of element to get.</typeparam>
        /// <param name="doc">The usual active document.</param>
        /// <param name="value">String value of parameter to filter by.</param>
        /// <param name="param">Guid or BuiltInParameter whose value to filter by.</param>
        /// <returns>A HashSet of revit objects already cast to the specified type.</returns>
        public static HashSet<T1> GetElements<T1, T2>(Document doc, string value, T2 param)
        {
            var parValFilter = Shared.Filter.ParameterValueGenericFilter(doc, value, param);
            return new FilteredElementCollector(doc).OfClass(typeof(T1)).WherePasses(parValFilter).Cast<T1>().ToHashSet();
        }
    }

    public static class MepUtils
    {
        public static IList<string> GetDistinctPhysicalPipingSystemTypeNames(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            HashSet<PipingSystem> pipingSystems = collector.OfClass(typeof(PipingSystem)).Cast<PipingSystem>().ToHashSet();
            HashSet<PipingSystemType> pipingSystemTypes = pipingSystems.Select(ps => doc.GetElement(ps.GetTypeId())).Cast<PipingSystemType>().ToHashSet();

            //Following code takes care if PCF_PIPL_EXCL has not been properly imported.
            PipingSystemType pstype = pipingSystemTypes.FirstOrDefault();
            if (pstype == null) throw new Exception("No piping systems created yet! Draw some pipes.");

            HashSet<string> abbreviations;
            //Do not allow systems with PCF_PIPL_EXCL if it exists!
            //GUID is defined in PCF_Exporter ParameterList!!!
            //Do not change w/o coordination
            if (pstype.get_Parameter(new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C")) == null)
            {
                //If parameter doesn't exist, get all systems
                abbreviations = pipingSystemTypes.Select(pst => pst.Abbreviation).ToHashSet();
            }
            else
            {
                //If parameter exists, take only not excluded
                abbreviations = pipingSystemTypes
                      .Where(pst => pst.get_Parameter(new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C")).AsInteger() == 0) //Filter out EXCLUDED piping systems
                      .Select(pst => pst.Abbreviation).ToHashSet();
            }

            return abbreviations.Distinct().ToList();
        }

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

            switch (element)
            {
                case MEPCurve pipe:
                    sysTypeId = pipe.MEPSystem.GetTypeId();
                    break;
                case FamilyInstance fi:
                    sysTypeId = fi.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
                    //ConnectorSet cSet = fi.MEPModel.ConnectorManager.Connectors;
                    //Connector con =
                    //    (from Connector c in cSet where c.GetMEPConnectorInfo().IsPrimary select c).FirstOrDefault();
                    //sysTypeId = con.MEPSystem.GetTypeId();
                    break;
                default:
                    throw new Exception("Trying to get PipingSystemType from nor MEPCurve nor FamilyInstance for element: " + element.Id.IntegerValue);
            }

            return (PipingSystemType)doc.GetElement(sysTypeId);
        }

        public static Cons GetConnectors(Element element) => new Cons(element);

        public static HashSet<Connector> GetAllConnectorsFromConnectorSet(ConnectorSet conSet)
        {
            IList<Connector> list = new List<Connector>();
            foreach (Connector con in conSet) list.Add(con);
            return list.ToHashSet();
        }

        public static ConnectorSet GetConnectorSet(Element e)
        {
            ConnectorSet cs = null;

            if (e is FamilyInstance)
            {
                MEPModel m = ((FamilyInstance)e).MEPModel;
                if (null != m && null != m.ConnectorManager) cs = m.ConnectorManager.Connectors;
            }

            else if (e is Wire) cs = ((Wire)e).ConnectorManager.Connectors;

            else
            {
                Debug.Assert(e.GetType().IsSubclassOf(typeof(MEPCurve)),
                    "expected all candidate connector provider "
                    + "elements to be either family instances or "
                    + "derived from MEPCurve");

                if (e is MEPCurve) cs = ((MEPCurve)e).ConnectorManager.Connectors;
            }

            return cs ?? new ConnectorSet();
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(HashSet<Element> elements)
        {
            return (from e in elements from Connector c in GetConnectorSet(e) select c).ToHashSet();
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(Element element)
        {
            return (from Connector c in GetConnectorSet(element) select c).ToHashSet();
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
            var connectors = MepUtils.GetALLConnectorsFromElements(element);

            switch (element)
            {
                case Pipe pipe:

                    var filteredCons = connectors.Where(c => c.ConnectorType.ToString() == "End").ToList();

                    Primary = filteredCons.First();
                    Secondary = filteredCons.Last();
                    break;

                case FamilyInstance fi:
                    foreach (Connector connector in connectors)
                    {
                        MEPConnectorInfo mci = connector.GetMEPConnectorInfo();

                        Count++;

                        if (mci.IsPrimary) Primary = connector;
                        else if (mci.IsSecondary) Secondary = connector;
                        else Tertiary = connector;
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
                    break;
                default:
                    throw new Exception("Cons: Element id nr.: " + element.Id.ToString() + " is not a Pipe or FamilyInstance!");
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
            //OleDbConnection con = null;
            DataTable dt = null;
            using (OleDbConnection con = new OleDbConnection(connectionString))
            {
                con.Open();
                dt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            }

            if (dt == null) return null;

            string[] excelSheetNames = new string[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                excelSheetNames[i] = row["TABLE_NAME"].ToString();
                i++;
            }

            return excelSheetNames;
        }

        public static DataTable ReadDataTable(DataTableCollection dataTableCollection, string tableName)
        {
            return (from DataTable dtbl in dataTableCollection where dtbl.TableName == tableName select dtbl).FirstOrDefault();
        }
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
            return Math.Round(a, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"));
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

        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
        }

        public static bool IsPipe(this Element elem)
        {
            switch (elem)
            {
                case Pipe pipe:
                    return true;
                default:
                    return false;
            }
        }
    }
}
