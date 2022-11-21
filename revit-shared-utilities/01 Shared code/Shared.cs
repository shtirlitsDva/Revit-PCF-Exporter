using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
//using MoreLinq;
using Shared.BuildingCoder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

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
                    FilterStringRule fsr = new FilterStringRule(pvp, fsrE, str);
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
        /// <param name="param">Guid or BuiltInParameter whose value to filter by OR BuiltInCategory OR INVALID</param>
        /// <returns>A HashSet of revit objects already cast to the specified type.</returns>
        public static HashSet<T1> GetElements<T1, T2>(Document doc, T2 param, string value = null, ElementId viewId = null)
        {
            if (value != null)
            {
                var parValFilter = Shared.Filter.ParameterValueGenericFilter(doc, value, param);
                return new FilteredElementCollector(doc).OfClass(typeof(T1)).WherePasses(parValFilter).Cast<T1>().ToHashSet();
            }

            if (viewId != null)
            {
                return new FilteredElementCollector(doc, viewId).OfClass(typeof(T1)).Cast<T1>().ToHashSet();
            }

            if (param is BuiltInCategory bic)
            {
                switch (bic)
                {
                    case BuiltInCategory.OST_PipeCurves:
                        return new FilteredElementCollector(doc).OfCategory(bic).OfClass(typeof(Pipe)).Cast<T1>().ToHashSet();
                    case BuiltInCategory.OST_PipeAccessory:
                    case BuiltInCategory.OST_PipeFitting:
                        return new FilteredElementCollector(doc).OfCategory(bic).OfClass(typeof(FamilyInstance)).Cast<T1>().ToHashSet();
                    case BuiltInCategory.OST_PipeInsulations:
                        return new FilteredElementCollector(doc).OfCategory(bic).OfClass(typeof(PipeInsulation)).Cast<T1>().ToHashSet();
                    case BuiltInCategory.INVALID:
                        return new FilteredElementCollector(doc).OfClass(typeof(T1)).Cast<T1>().ToHashSet();
                    default:
                        return new FilteredElementCollector(doc).OfCategory(bic).OfClass(typeof(T1)).Cast<T1>().ToHashSet();
                        //throw new NotImplementedException($"The {bic} is not implemented in the GetElements method!");
                }
            }

            return new FilteredElementCollector(doc).OfClass(typeof(T1)).Cast<T1>().ToHashSet();
        }

        public static HashSet<Element> GetElements(Document doc, List<BuiltInCategory> categoriesList, List<Type> typesList)
        {
            HashSet<Element> result = new HashSet<Element>();
            for (int i = 0; i < categoriesList.Count; i++)
            {
                result.UnionWith(new FilteredElementCollector(doc).OfCategory(categoriesList[i]).OfClass(typesList[i]).ToHashSet());
            }
            return result;
        }

        /// <summary>
        /// Return a view, specify the type of view and name.
        /// </summary>
        /// <typeparam name="T">Type of view needed.</typeparam>
        /// <param name="name">The name of view needed.</param>
        /// <param name="doc">Standard Document.</param>
        /// <returns></returns>
        public static T GetViewByName<T>(string name, Document doc) where T : Element
        {
            return
                (from v in GetElements<View, object>(doc, BuiltInCategory.INVALID) where v != null && !v.IsTemplate && v.Name == name select v as T)
                    .FirstOrDefault();
        }

        /// <summary>
        /// Return a 3D view from the given document.
        /// </summary>
        public static View3D Get3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(View3D));

            foreach (View3D v in collector)
            {
                // skip view templates here because they
                // are invisible in project browsers:

                if (v != null && !v.IsTemplate && v.Name == "{3D}")
                {
                    return v;
                }
            }
            return null;
        }

        public static LogicalOrFilter FamSymbolsAndPipeTypes()
        {
            BuiltInCategory[] bics =
            {
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
            };

            IList<ElementFilter> a = new List<ElementFilter>(bics.Length);

            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            LogicalAndFilter familySymbolFilter = new LogicalAndFilter(categoryFilter,
                new ElementClassFilter(typeof(FamilySymbol)));

            IList<ElementFilter> b = new List<ElementFilter>
            {
                new ElementClassFilter(typeof(PipeType)),

                familySymbolFilter
            };
            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            return classFilter;
        }

        public static LogicalOrFilter FamInstOfDetailComp()
        {
            BuiltInCategory[] bics =
            {
                BuiltInCategory.OST_DetailComponents,
            };

            IList<ElementFilter> a = new List<ElementFilter>(bics.Length);

            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            LogicalAndFilter familySymbolFilter = new LogicalAndFilter(categoryFilter,
                new ElementClassFilter(typeof(FamilyInstance)));

            IList<ElementFilter> b = new List<ElementFilter>
            {
                familySymbolFilter
            };
            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            return classFilter;
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

        public static HashSet<Connector> GetALLConnectorsInDocument(Document doc, bool includeMechEquipment = false)
        {
            return (from e in Filter.GetElementsWithConnectors(doc, includeMechEquipment) from Connector c in GetConnectorSet(e) select c).ToHashSet();
        }

        /// <summary>
        /// Return a hash string for an XYZ point or vector with its coordinates formatted to 2 decimal places.
        /// Connectors can have tolerance which exceeds 3 decimal places.
        /// </summary>
        public static string HashStringForConnectorXYZ(XYZ p)
        {
            return string.Format("({0},{1},{2})", p.X.ToString("0.##"), p.Y.ToString("0.##"), p.Z.ToString("0.##"));
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(HashSet<Element> elements, IEqualityComparer<Connector> comparer)
        {
            return (from e in elements from Connector c in Shared.MepUtils.GetConnectorSet(e) select c).ToHashSet(comparer);
        }

        public static bool IsTheElementACap(Element element)
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

        public static Dictionary<int, double> pipeWallThkDict()
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

        public static Dictionary<int, double> outerDiaDict()
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

    /// <summary>
    /// Comparer for Connector objects based on their geometric location within a set tolerance.
    /// Tolerance must be in feet.
    /// </summary>
    public class ConnectorXyzComparer : IEqualityComparer<Connector>
    {
        double Tol = Extensions._1mmTol;

        public ConnectorXyzComparer(double tol)
        {
            Tol = tol;
        }

        public bool Equals(Connector x, Connector y) => null != x && null != y && x.Equalz(y, Tol);

        public int GetHashCode(Connector x) => MepUtils.HashStringForConnectorXYZ(x.Origin).GetHashCode();
    }

    /// <summary>
    /// Comparer for XYZ points based on their geometric location within a set tolerance.
    /// Tolerance must be in feet.
    /// </summary>
    public class XyzComparer : IEqualityComparer<XYZ>
    {
        double Tol = 1e-6.MmToFt();

        public XyzComparer() { }

        public XyzComparer(double tol) => Tol = tol;

        public bool Equals(XYZ x, XYZ y) => null != x && null != y && x.Equalz(y, Tol);

        public int GetHashCode(XYZ x) => Tuple.Create(
            Math.Round(x.X, 6), Math.Round(x.Y, 6), Math.Round(x.Z, 6)).GetHashCode();
    }

    public class Cons
    {
        public Connector Primary { get; } = null;
        public Connector Secondary { get; } = null;
        public Connector Tertiary { get; } = null;
        public int Count { get; } = 0;
        public Connector Largest { get; } = null;
        public Connector Smallest { get; } = null;

        public Cons(Element element, bool pipeConnectorsOrdered = false)
        {
            var connectors = MepUtils.GetALLConnectorsFromElements(element);

            switch (element)
            {
                case Pipe pipe:

                    List<Connector> filteredCons = new List<Connector>();
                    if (pipeConnectorsOrdered)
                    {
                        filteredCons = connectors
                            .Where(c => c.ConnectorType.ToString() == "End")
                            .OrderBy(c => c.Origin.X.Round(1))
                            .ThenBy(c => c.Origin.Y.Round(1))
                            .ThenBy(c => c.Origin.Z.Round(1))
                            .ToList();
                    }
                    else filteredCons = connectors.Where(c => c.ConnectorType.ToString() == "End").ToList();

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
                    "provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0;HDR={1};\"",
                    fileName, dataHasHeaders);

            DataSet data = new DataSet();

            foreach (string sheetName in GetExcelSheetNames(connectionString))
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    var dataTable = new DataTable();
                    string query = string.Format("SELECT * FROM [{0}]", sheetName);

                    try
                    {
                        con.Open();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"OleDb threw an Exception AGAIN!!!\n{e.Message}");
                    }

                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, con);
                    adapter.Fill(dataTable);

                    //Remove ' and $ from sheetName
                    Regex rgx = new Regex("[^a-zA-Z0-9 _-]");
                    string tableName = rgx.Replace(sheetName, "");

                    dataTable.TableName = tableName;
                    data.Tables.Add(dataTable);
                }
            }

            if (data == null) BuildingCoderUtilities.ErrorMsg("Data set is null");
            if (data.Tables.Count < 1) BuildingCoderUtilities.ErrorMsg("Table count in DataSet is 0");

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

        public static string ReadParameterFromDataTable(string key, DataTable table, string parameter)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(0) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(0) == key
                            select row.Field<string>(parameter);

                string value = query.FirstOrDefault();

                //if (value.IsNullOrEmpty()) return null;
                return value;
            }
            else return null;
        }
        /// <summary>
        /// Looks up value from data table.
        /// </summary>
        /// <param name="key">Value to match in key column</param>
        /// <param name="table">Table to look up in</param>
        /// <param name="parameter">Column name where to get return value</param>
        /// <param name="keyColumnIdx">Key column index</param>
        /// <returns></returns>
        public static string ReadStringParameterFromDataTable(string key, System.Data.DataTable table, string parameter, int keyColumnIdx)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(keyColumnIdx) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(keyColumnIdx) == key
                            select row.Field<string>(parameter);

                string value = query.FirstOrDefault();

                //if (value.IsNullOrEmpty()) return null;
                return value;
            }
            else return null;
        }
        /// <summary>
        /// Looks up value from data table.
        /// </summary>
        /// <param name="key">Value to match in key column</param>
        /// <param name="table">Table to look up in</param>
        /// <param name="parameter">Column name where to get return value</param>
        /// <param name="keyColumnIdx">Key column index</param>
        /// <returns></returns>
        public static double ReadDoubleParameterFromDataTable(string key, System.Data.DataTable table, string parameter, int keyColumnIdx)
        {
            //Test if value exists
            if (table.AsEnumerable().Any(row => row.Field<string>(keyColumnIdx) == key))
            {
                var query = from row in table.AsEnumerable()
                            where row.Field<string>(keyColumnIdx) == key
                            select row.Field<string>(parameter);

                string value = query.FirstOrDefault();

                if (value.IsNoE() || value == null) return 0;

                double result;

                if (double.TryParse(value, NumberStyles.AllowDecimalPoint,
                                    CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                return 0;
            }
            else return 0;
        }

        public static DataTable READExcel(string path)
        {
            Microsoft.Office.Interop.Excel.Application objXL = null;
            Microsoft.Office.Interop.Excel.Workbook objWB = null;
            objXL = new Microsoft.Office.Interop.Excel.Application();
            objWB = objXL.Workbooks.Open(path);
            Microsoft.Office.Interop.Excel.Worksheet objSHT = objWB.Worksheets[1];

            int rows = objSHT.UsedRange.Rows.Count;
            int cols = objSHT.UsedRange.Columns.Count;
            DataTable dt = new DataTable();
            int noofrow = 1;

            for (int c = 1; c <= cols; c++)
            {
                string colname = objSHT.Cells[1, c].Text;
                dt.Columns.Add(colname);
                noofrow = 2;
            }

            for (int r = noofrow; r <= rows; r++)
            {
                DataRow dr = dt.NewRow();
                for (int c = 1; c <= cols; c++)
                {
                    dr[c - 1] = objSHT.Cells[r, c].Text;
                }

                dt.Rows.Add(dr);
            }

            objWB.Close();
            objXL.Quit();
            return dt;
        }

        internal static DataTable ReadCsvToDataTable(string path, string dataTableName)
        {
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { ";" });
                csvParser.HasFieldsEnclosedInQuotes = false;

                string[] colNames = new string[0];
                string[] fields = new string[0];

                DataTable dt = new DataTable(dataTableName);

                int counter = 0;
                while (!csvParser.EndOfData)
                {
                    if (counter == 0)
                    {
                        colNames = csvParser.ReadFields();
                        counter++;

                        for (int i = 0; i < colNames.Length; i++)
                        {

                            DataColumn dc = new DataColumn(colNames[i]);
                            dc.DataType = typeof(string);
                            dt.Columns.Add(dc);
                        }
                    }
                    else
                    {// Read current line fields, pointer moves to the next line.
                        fields = csvParser.ReadFields();
                        DataRow dr = dt.NewRow();

                        for (int i = 0; i < colNames.Length; i++)
                        {
                            dr[i] = fields[i];
                        }
                        dt.Rows.Add(dr);
                        counter++;
                    }
                }
                return dt;
            }
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

        public static double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }

    public static class Extensions
    {
        public const double _epx = 1.0e-9; //Original tolerance
        public const double _1mmTol = 0.00328; //Tolerance roughly 1 mm
        const double _inch_to_mm = 25.4;
        const double _foot_to_mm = 12 * _inch_to_mm;
        const double _foot_to_inch = 12;
        const double _convertFootToMeter = _foot_to_mm * 0.001;
        const double _convertSqrFootToSqrMeter = _convertFootToMeter * _convertFootToMeter;
        const double _convertCubicFootToCubicMeter = _convertFootToMeter * _convertFootToMeter * _convertFootToMeter;
        public static double Round(this double number, int decimals = 0)
        {
            return Math.Round(number, decimals, MidpointRounding.AwayFromZero);
        }
        public static double FtToMm(this double l) => l * _foot_to_mm;
        /// <summary>
        /// Returns the value converted to meters.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static double FtToMtrs(this double l) => l * _foot_to_mm / 1000;
        public static double FtToInch(this double l) => l * _foot_to_inch;
        public static double MmToFt(this double l) => l / _foot_to_mm;
        public static double SqrFeetToSqrMeters(this double l) => l * _convertSqrFootToSqrMeter;
        public static double CubicFtPrSecToCubicMtrPrHour(this double f) =>
            f * 0.02831685 * 3600.0;
        public static bool Equalz(this double a, double b, double tol) => Math.Abs(a - b) <= tol;
        public static bool Equalz(this XYZ a, XYZ b, double tol) => a.X.Equalz(b.X, tol) && a.Y.Equalz(b.Y, tol) && a.Z.Equalz(b.Z, tol);
        public static bool Equalz(this Connector a, Connector b, double tol) => a.Origin.Equalz(b.Origin, tol);
        public static bool IsOdd(this int number) => number % 2 != 0;
        public static bool IsType<T>(this object obj) => obj is T;
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNoE(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNotNoE(this string str) => !string.IsNullOrEmpty(str);
        public static IEnumerable<T> ExceptWhere<T>(this IEnumerable<T> source, Predicate<T> predicate) => source.Where(x => !predicate(x));
        public static string FamilyName(this Element e) => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
        public static string FamilyAndTypeName(this Element e) => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();
        public static string MEPSystemAbbreviation(this Connector con, Document doc, bool ignoreMepSystemNull = false)
        {
            if (con.MEPSystem != null)
            {
                MEPSystem ps = con.MEPSystem;
                PipingSystemType pst = (PipingSystemType)doc.GetElement(ps.GetTypeId());
                return pst.Abbreviation;
            }
            else if (ignoreMepSystemNull) return "";
            else throw new Exception($"A connector at element {con.Owner.Id.IntegerValue} has MEPSystem = null!");
        }
        public static string MEPSystemAbbreviation(this Element elem)
        {
            Parameter abbreviationParameter = elem.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM);
            return abbreviationParameter.AsString();
        }
        /// <summary>
        /// Returns, for fittings only, the PartType of the element in question.
        /// </summary>
        /// <param name="e">Element to get the PartType property.</param>
        /// <returns>The PartType of the passed element.</returns>
        public static PartType MechFittingPartType(this Element e)
        {
            if (e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
            {
                var mf = ((FamilyInstance)e).MEPModel as MechanicalFitting;
                return mf.PartType;
            }
            else return PartType.Undefined;
        }
        public static string ComponentClass1(this Element e, Document doc)
        {
            Element elType = doc.GetElement(e.GetTypeId());
            Parameter par = elType.get_Parameter(new Guid("a7f72797-135b-4a1c-8969-e2e3fc76ff14")); //Component Class 1
            if (par == null) return "";
            return par.AsString();
        }
        /// <summary>
        /// Remember to check for con.IsConnected!
        /// </summary>
        /// <param name="con">Connector for which to get the correspoinding ref con.</param>
        /// <param name="elem">The host element of the original connector.</param>
        /// <returns>The corresponding reference connector.</returns>
        public static Connector GetRefConnnector(this Connector con, Element elem)
        {
            var allRefsNotFiltered = MepUtils.GetAllConnectorsFromConnectorSet(con.AllRefs);
            var correspondingCon = allRefsNotFiltered
                .Where(x => x.Domain == Domain.DomainPiping)
                .Where(x => x.Owner.Id.IntegerValue != elem.Id.IntegerValue).FirstOrDefault();
            return correspondingCon;
        }
        /// <summary>
        /// Method is taken from here:
        /// https://spiderinnet.typepad.com/blog/2011/08/revit-parameter-api-asvaluestring-tostring-tovaluestring-and-tovaluedisplaystring.html
        /// </summary>
        /// <param name="p">Revit parameter</param>
        /// <returns>Stringified contents of the parameter</returns>
        public static string ToValueString(this Autodesk.Revit.DB.Parameter p)
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
        public static string ToValueString2(this Autodesk.Revit.DB.Parameter p)
        {
            string ret = string.Empty;

            switch (p.StorageType)
            {
                case StorageType.ElementId:
                    ret = p.AsValueString();
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
        /// <summary>	
        /// Return a descriptive string for a built-in 	
        /// category by removing the trailing plural 's' 	
        /// and the OST_ prefix.	
        /// </summary>	
        public static string Description(this BuiltInCategory bic)
        {
            string s = bic.ToString().ToLower();
            s = s.Substring(4);
            Debug.Assert(s.EndsWith("s"), "expected plural suffix 's'");
            s = s.Substring(0, s.Length - 1);
            return s;
        }
        internal static string MultipleInstanceParameterValuesAsString(this Element element,
                                                    string[] parNames, string separator = "")
        {
            List<string> values = new List<string>();
            foreach (string name in parNames)
            {
                string value = element.LookupParameter(name)?.ToValueString2();
                if (!value.IsNullOrEmpty()) values.Add(value);
            }
            if (values.Count < 1) return "";
            else return $"{string.Join(separator, values)}";
        }
        internal static string MultipleTypeParameterValuesAsString(this Element element, Document doc,
                                                    string[] parNames, string separator = "")
        {
            Element elType = doc.GetElement(element.GetTypeId());
            List<string> values = new List<string>();
            foreach (string name in parNames)
            {
                string value = elType.LookupParameter(name)?.ToValueString2();
                if (!value.IsNullOrEmpty()) values.Add(value);
            }
            if (values.Count < 1) return "";
            else return $"{string.Join(separator, values)}";
        }
        /// <summary>
        /// Returns pipe length in meters.
        /// </summary>
        public static double Length(this Pipe pipe) =>
            pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble().FtToMtrs();
    }

    public static class Transformation
    {
        public static void RotateElementInPosition(XYZ placementPoint, Connector conOnFamilyToConnect, Connector start, Element element)
        {
            #region Geometric manipulation

            //http://thebuildingcoder.typepad.com/blog/2012/05/create-a-pipe-cap.html

            //Select the OTHER connector
            MEPCurve hostPipe = start.Owner as MEPCurve;

            Connector end = (from Connector c in hostPipe.ConnectorManager.Connectors //End of the host/dummy pipe
                             where c.Id != start.Id && (int)c.ConnectorType == 1
                             select c).FirstOrDefault();

            XYZ dir = (start.Origin - end.Origin);

            // rotate the cap if necessary
            // rotate about Z first

            XYZ pipeHorizontalDirection = new XYZ(dir.X, dir.Y, 0.0).Normalize();
            //XYZ pipeHorizontalDirection = new XYZ(dir.X, dir.Y, 0.0);

            XYZ connectorDirection = -conOnFamilyToConnect.CoordinateSystem.BasisZ;

            double zRotationAngle = pipeHorizontalDirection.AngleTo(connectorDirection);

            Transform trf = Transform.CreateRotationAtPoint(XYZ.BasisZ, zRotationAngle, placementPoint);

            XYZ testRotation = trf.OfVector(connectorDirection).Normalize();

            if (Math.Abs(testRotation.DotProduct(pipeHorizontalDirection) - 1) > 0.00001)
                zRotationAngle = -zRotationAngle;

            Line axis = Line.CreateBound(placementPoint, placementPoint + XYZ.BasisZ);

            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, zRotationAngle);

            //Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            //comments.Set("Horizontal only");

            // Need to rotate vertically?

            if (Math.Abs(dir.DotProduct(XYZ.BasisZ)) > 0.000001)
            {
                // if pipe is straight up and down, 
                // kludge it my way else

                if (dir.X.Round(3) == 0 && dir.Y.Round(3) == 0 && dir.Z.Round(3) != 0)
                {
                    XYZ yaxis = new XYZ(0.0, 1.0, 0.0);
                    //XYZ yaxis = dir.CrossProduct(connectorDirection);

                    double rotationAngle = dir.AngleTo(yaxis);
                    //double rotationAngle = 90;

                    if (dir.Z.Equals(1)) rotationAngle = -rotationAngle;

                    axis = Line.CreateBound(placementPoint,
                        new XYZ(placementPoint.X, placementPoint.Y + 5, placementPoint.Z));

                    ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

                    //comments.Set("Vertical!");
                }
                else
                {
                    #region sloped pipes

                    double rotationAngle = dir.AngleTo(pipeHorizontalDirection);

                    XYZ normal = pipeHorizontalDirection.CrossProduct(XYZ.BasisZ);

                    trf = Transform.CreateRotationAtPoint(normal, rotationAngle, placementPoint);

                    testRotation = trf.OfVector(dir).Normalize();

                    if (Math.Abs(testRotation.DotProduct(pipeHorizontalDirection) - 1) < 0.00001)
                        rotationAngle = -rotationAngle;

                    axis = Line.CreateBound(placementPoint, placementPoint + normal);

                    ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

                    //comments.Set("Sloped");

                    #endregion
                }
            }

            #endregion
        }
    }

    public static class Output
    {
        public static void WriteDebugFile<T>(string filePath, T whatToWrite)
        {
            // Clear the output file
            System.IO.File.WriteAllBytes(filePath, new byte[0]);

            using (StreamWriter w = File.AppendText(filePath))
            {
                if (whatToWrite is StringBuilder sb) w.Write(sb.ToString());
                else w.Write(whatToWrite);
                w.Close();
            }
        }
    }

    public class Dbg
    {
        /// <summary>
        /// This method is used to place an adaptive family which helps in debugging
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static FamilyInstance PlaceAdaptiveFamilyInstance(Document doc, string famAndTypeName, XYZ p1, XYZ p2)
        {
            //Get the symbol
            ElementParameterFilter filter = Filter.ParameterValueGenericFilter(doc, famAndTypeName,
                BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM); //Hardcoded until implements

            FamilySymbol markerSymbol =
                new FilteredElementCollector(doc).WherePasses(filter)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();

            // Create a new instance of an adaptive component family
            FamilyInstance instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc,
                markerSymbol);

            // Get the placement points of this instance
            IList<ElementId> placePointIds = new List<ElementId>();
            placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
            // Set the position of each placement point
            ReferencePoint point1 = doc.GetElement(placePointIds[0]) as ReferencePoint;
            point1.Position = p1;
            ReferencePoint point2 = doc.GetElement(placePointIds[1]) as ReferencePoint;
            point2.Position = p2;

            return instance;
        }
    }
}
