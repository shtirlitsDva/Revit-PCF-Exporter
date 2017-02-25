using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms.ComponentModel.Com2Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using BuildingCoder;
using MoreLinq;
using iv = PCF_Functions.InputVars;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Functions
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

    public class Composer
    {
        #region Preamble
        //PCF Preamble composition

        public StringBuilder PreambleComposer()
        {
            StringBuilder sbPreamble = new StringBuilder();
            sbPreamble.Append("ISOGEN-FILES ISOCONFIG.FLS");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BORE " + InputVars.UNITS_BORE);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-CO-ORDS " + InputVars.UNITS_CO_ORDS);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-WEIGHT " + InputVars.UNITS_WEIGHT);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BOLT-DIA MM");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BOLT-LENGTH MM");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-WEIGHT-LENGTH " + InputVars.UNITS_WEIGHT_LENGTH);
            sbPreamble.AppendLine();
            return sbPreamble;
        }
        #endregion

        #region Materials section
        public StringBuilder MaterialsSection(IEnumerable<IGrouping<string, Element>> elementGroups)
        {
            StringBuilder sbMaterials = new StringBuilder();
            int groupNumber = 0;
            sbMaterials.Append("MATERIALS");
            foreach (IGrouping<string, Element> group in elementGroups)
            {
                groupNumber++;
                sbMaterials.AppendLine();
                sbMaterials.Append("MATERIAL-IDENTIFIER " + groupNumber);
                sbMaterials.AppendLine();
                sbMaterials.Append("    DESCRIPTION " + group.Key);
            }
            return sbMaterials;
        }
        #endregion

        #region CII export writer

        public static StringBuilder CIIWriter(Document document, string systemAbbreviation)
        {
            StringBuilder sbCII = new StringBuilder();
            //Handle CII export parameters
            //Instantiate collector
            FilteredElementCollector collector = new FilteredElementCollector(document);
            //Get the elements
            PipingSystemType sQuery = collector.OfClass(typeof(PipingSystemType))
                .WherePasses(Filter.ParameterValueFilter(systemAbbreviation, BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM))
                .Cast<PipingSystemType>()
                .FirstOrDefault();

            ////Select correct systemType
            //PipingSystemType sQuery = (from PipingSystemType st in collector
            //                           where string.Equals(st.Abbreviation, systemAbbreviation)
            //                           select st).FirstOrDefault();

            var query = from p in new plst().ListParametersAll
                        where string.Equals(p.Domain, "PIPL") && string.Equals(p.ExportingTo, "CII")
                        select p;

            foreach (pdef p in query.ToList())
            {
                if (string.IsNullOrEmpty(sQuery.get_Parameter(p.Guid).AsString())) continue;
                sbCII.AppendLine("    " + p.Keyword + " " + sQuery.get_Parameter(p.Guid).AsString());
            }

            return sbCII;
        }

        #endregion

        #region Plant 3D Iso Writer
        /// <summary>
        /// Method to write ITEM-CODE and ITEM-DESCRIPTION entries to enable import of the PCF file into the Plant 3D "PLANTPCFTOISO" command.
        /// </summary>
        /// <param name="element">The current element being written.</param>
        /// <returns>StringBuilder containing the entries.</returns>
        public static StringBuilder Plant3DIsoWriter(Element element, Document doc)
        {
            //If an element has EXISTING in it's PCF_ELEM_SPEC the writing of ITEM-CODE will be skipped, making Plant 3D ISO treat it as existing.
            pdef elemSpec = new plst().PCF_ELEM_SPEC;
            Parameter pm = element.get_Parameter(elemSpec.Guid);
            if (string.Equals(pm.AsString(), "EXISTING")) return new StringBuilder();

            //Write ITEM-CODE et al
            StringBuilder sb = new StringBuilder();

            pdef matId = new plst().PCF_MAT_ID;
            pdef matDescr = new plst().PCF_MAT_DESCR;

            int itemCode = element.get_Parameter(matId.Guid).AsInteger();
            string itemDescr = element.get_Parameter(matDescr.Guid).AsString();
            string key = MepUtils.GetElementPipingSystemType(element, doc).Abbreviation;

            sb.AppendLine("    ITEM-CODE " + key + "-" + itemCode);
            sb.AppendLine("    ITEM-DESCRIPTION " + itemDescr);

            return sb;
        }

        #endregion

        #region ELEM parameter writer
        public StringBuilder ElemParameterWriter(Element passedElement)
        {
            StringBuilder sbElemParameters = new StringBuilder();

            var pQuery = from p in new plst().ListParametersAll
                         where !string.IsNullOrEmpty(p.Keyword) && string.Equals(p.Domain, "ELEM")
                         select p;

            foreach (pdef p in pQuery)
            {
                //Check for parameter's storage type (can be Int for select few parameters)
                int sT = (int)passedElement.get_Parameter(p.Guid).StorageType;

                if (sT == 1)
                {
                    //Check if the parameter contains anything
                    if (string.IsNullOrEmpty(passedElement.get_Parameter(p.Guid).AsInteger().ToString())) continue;
                    sbElemParameters.Append("    " + p.Keyword + " ");
                    sbElemParameters.Append(passedElement.get_Parameter(p.Guid).AsInteger());
                }
                else if (sT == 3)
                {
                    //Check if the parameter contains anything
                    if (string.IsNullOrEmpty(passedElement.get_Parameter(p.Guid).AsString())) continue;
                    sbElemParameters.Append("    " + p.Keyword + " ");
                    sbElemParameters.Append(passedElement.get_Parameter(p.Guid).AsString());
                }
                sbElemParameters.AppendLine();
            }
            return sbElemParameters;
        }
        #endregion
    }

    public class Filter
    {
        public static ElementParameterFilter ParameterValueFilter(string valueQualifier, BuiltInParameter parameterName)
        {
            BuiltInParameter testParam = parameterName;
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)testParam));
            FilterStringRuleEvaluator str = new FilterStringContains();
            FilterStringRule paramFr = new FilterStringRule(pvp, str, valueQualifier, false);
            ElementParameterFilter epf = new ElementParameterFilter(paramFr);
            return epf;
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
            IList<ElementFilter> b = new List<ElementFilter>();

            //b.Add(new ElementClassFilter(typeof(CableTray)));
            //b.Add(new ElementClassFilter(typeof(Conduit)));
            //b.Add(new ElementClassFilter(typeof(Duct)));
            b.Add(new ElementClassFilter(typeof(Pipe)));

            b.Add(familyInstanceFilter);

            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }
    }

    public class FilterDiameterLimit
    {
        private Element element;
        private bool diameterLimitBool;
        private double diameterLimit;
        /// <summary>
        /// Tests the diameter of the pipe or primary connector of element against the diameter limit set in the interface.
        /// </summary>
        /// <param name="passedElement"></param>
        /// <returns>True if diameter is larger than limit and false if smaller.</returns>
        public bool FilterDL(Element passedElement)
        {
            element = passedElement;
            diameterLimit = iv.DiameterLimit;
            diameterLimitBool = true;
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
                    //Cast the element passed to method to FamilyInstance
                    FamilyInstance familyInstance = (FamilyInstance)element;
                    //MEPModel of the elements is accessed
                    MEPModel mepmodel = familyInstance.MEPModel;
                    //Get connector set for the element
                    ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;
                    //Declare a variable for 
                    Connector testedConnector = null;

                    if (connectorSet.IsEmpty) break;
                    if (connectorSet.Size == 1) foreach (Connector connector in connectorSet) testedConnector = connector;
                    else testedConnector = (from Connector connector in connectorSet
                                            where connector.GetMEPConnectorInfo().IsPrimary
                                            select connector).FirstOrDefault();

                    if (iv.UNITS_BORE_MM) testedDiameter = double.Parse(Conversion.PipeSizeToMm(testedConnector.Radius));
                    else if (iv.UNITS_BORE_INCH) testedDiameter = double.Parse(Conversion.PipeSizeToInch(testedConnector.Radius));

                    if (testedDiameter <= diameterLimit) diameterLimitBool = false;

                    break;
            }
            return diameterLimitBool;
        }
    }

    public static class Conversion
    {
        const double _inch_to_mm = 25.4;
        const double _foot_to_mm = 12 * _inch_to_mm;
        const double _foot_to_inch = 12;

        /// <summary>
        /// Return a string for a real number formatted to two decimal places.
        /// </summary>
        private static string RealString(double a)
        {
            //return a.ToString("0.##");
            //return (Math.Truncate(a * 100) / 100).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"));
            return Math.Round(a, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.GetCultureInfo("en-GB"));
        }

        /// <summary>
        /// Return a string for an XYZ point or vector with its coordinates converted from feet to millimetres and formatted to two decimal places.
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

        public static double Round2(this Double number)
        {
            return Math.Round(number, 2, MidpointRounding.AwayFromZero);
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
                var query = from p in new plst().ListParametersAll where p.Usage == curUsage && p.Domain == curDomain select p;

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
        public static DataSet ImportExcelToDataSet(string fileName)
        {
            //On connection strings http://www.connectionstrings.com/excel/#p84
            string connectionString =
                string.Format(
                    "provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"",
                    fileName);

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

    public static class ParameterDataWriter
    {
        public static void SetWallThicknessPipes(HashSet<Element> elements)
        {
            //Wallthicknes for pipes are hardcoded until further notice
            //Values are from 10216-2 - Seamless steel tubes for pressure purposes
            //TODO: Implement a way to read values from excel
            Dictionary<int, string> pipeWallThk = new Dictionary<int, string>
            {
                [10] = "1.8 mm",
                [15] = "2 mm",
                [20] = "2.3 mm",
                [25] = "2.6 mm",
                [32] = "2.6 mm",
                [40] = "2.6 mm",
                [50] = "2.9 mm",
                [65] = "2.9 mm",
                [80] = "3.2 mm",
                [100] = "3.6 mm",
                [125] = "4 mm",
                [150] = "4.5 mm",
                [200] = "6.3 mm",
                [250] = "6.3 mm",
                [300] = "7.1 mm",
                [350] = "8 mm",
                [400] = "8.8 mm",
                [450] = "10 mm",
                [500] = "11 mm",
                [600] = "12.5 mm"
            };

            pdef wallThkDef = new plst().PCF_ELEM_CII_WALLTHK;
            pdef elemType = new plst().PCF_ELEM_TYPE;

            foreach (Element element in elements)
            {
                //See if the parameter already has value and skip element if it has
                if (!string.IsNullOrEmpty(element.get_Parameter(wallThkDef.Guid).AsString())) continue;
                if (element.get_Parameter(elemType.Guid).AsString().Equals("SUPPORT")) continue;

                //Retrieve the correct wallthickness from dictionary and set it on the element
                Parameter wallThkParameter = element.get_Parameter(wallThkDef.Guid);

                //Get connector set for the pipes
                ConnectorSet connectorSet = MepUtils.GetConnectorManager(element).Connectors;

                Connector c1 = null;

                if (element is Pipe)
                {
                    //Filter out non-end types of connectors
                    c1 = (from Connector connector in connectorSet
                          where connector.ConnectorType.ToString().Equals("End")
                          select connector).FirstOrDefault();
                }

                if (element is FamilyInstance)
                {
                    c1 = (from Connector connector in connectorSet
                          where connector.GetMEPConnectorInfo().IsPrimary
                          select connector).FirstOrDefault();
                    Connector c2 = (from Connector connector in connectorSet
                                    where connector.GetMEPConnectorInfo().IsSecondary
                                    select connector).FirstOrDefault();
                    if (c2 != null)
                    {
                        if (c1.Radius > c2.Radius) c1 = c2;
                    }
                }

                string data = string.Empty;
                string source = Conversion.PipeSizeToMm(c1.Radius);
                int dia = Convert.ToInt32(source);
                pipeWallThk.TryGetValue(dia, out data);
                wallThkParameter.Set(data);
            }
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

            if (element is MEPCurve)
            {
                MEPCurve pipe = (MEPCurve)element;
                sysTypeId = pipe.MEPSystem.GetTypeId();
            }
            else if (element is FamilyInstance)
            {
                FamilyInstance famInst = (FamilyInstance)element;
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
            HashSet<string> abbreviations = pipingSystemTypes.Select(pst => pst.Abbreviation).ToHashSet();

            return abbreviations.Distinct().ToList();
        }
    }
}