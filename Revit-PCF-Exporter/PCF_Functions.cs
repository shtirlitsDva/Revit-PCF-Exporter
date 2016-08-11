using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using BuildingCoder;
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
        public static bool ExportAll = true;
        public static double DiameterLimit = 0;

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
        readonly StringBuilder sbPreamble = new StringBuilder();
        public StringBuilder PreambleComposer()
        {
            sbPreamble.Append("ISOGEN-FILES ISOGEN.FLS");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BORE "+InputVars.UNITS_BORE);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-CO-ORDS "+InputVars.UNITS_CO_ORDS);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-WEIGHT "+InputVars.UNITS_WEIGHT);
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BOLT-DIA MM");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-BOLT-LENGTH MM");
            sbPreamble.AppendLine();
            sbPreamble.Append("UNITS-WEIGHT-LENGTH "+InputVars.UNITS_WEIGHT_LENGTH);
            sbPreamble.AppendLine();
            return sbPreamble;
        }
        #endregion

        #region Materials section
        StringBuilder sbMaterials = new StringBuilder();
        IEnumerable<IGrouping<string, Element>> materialGroups = null;
        int groupNumber;
        public StringBuilder MaterialsSection(IEnumerable<IGrouping<string, Element>> elementGroups)
        {
            materialGroups = elementGroups;
            sbMaterials.Append("MATERIALS");
            foreach (IGrouping<string, Element> group in materialGroups)
            {
                groupNumber++;
                sbMaterials.AppendLine();
                sbMaterials.Append("MATERIAL-IDENTIFIER " + groupNumber);
                sbMaterials.AppendLine();
                sbMaterials.Append("    DESCRIPTION "+group.Key);
            }
            return sbMaterials;
        }
        #endregion

        #region CII export writer
        StringBuilder sbCII;
        private Document doc;
        private string key;

        public StringBuilder CIIWriter(Document document, string systemAbbreviation)
        {
            doc = document;
            key = systemAbbreviation;
            sbCII = new StringBuilder();
            //Handle CII export parameters
            //Instantiate collector
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            //Get the elements
            collector.OfClass(typeof (PipingSystemType));
            //Select correct systemType
            PipingSystemType sQuery = (from PipingSystemType st in collector
                where string.Equals(st.Abbreviation, key)
                select st).FirstOrDefault();

            var query = from p in new plst().ListParametersAll
                where string.Equals(p.Domain, "PIPL") && string.Equals(p.ExportingTo, "CII")
                select p;

            foreach (pdef p in query.ToList())
            {
                if (string.IsNullOrEmpty(sQuery.get_Parameter(p.Guid).AsString())) continue;
                sbCII.Append("    ");
                sbCII.Append(p.Keyword);
                sbCII.Append(" ");
                sbCII.Append(sQuery.get_Parameter(p.Guid).AsString());
                sbCII.AppendLine();
            }

            return sbCII;
        }

        #endregion

        #region ELEM parameter writer
        private StringBuilder sbElemParameters;
        private Element element;

        public StringBuilder ElemParameterWriter(Element passedElement)
        {
            sbElemParameters = new StringBuilder();
            element = passedElement;
            var pQuery = from p in new plst().ListParametersAll
                where !string.IsNullOrEmpty(p.Keyword) && string.Equals(p.Domain, "ELEM")
                select p;

            foreach (pdef p in pQuery)
            {
                //Check for parameter's storage type (can be Int for select few parameters)
                int sT = (int) element.get_Parameter(p.Guid).StorageType;

                if (sT == 1)
                {
                    //Check if the parameter contains anything
                    if (string.IsNullOrEmpty(element.get_Parameter(p.Guid).AsInteger().ToString())) continue;
                    sbElemParameters.Append("    " + p.Keyword + " ");
                    sbElemParameters.Append(element.get_Parameter(p.Guid).AsInteger());
                }
                else if (sT == 3)
                {
                    //Check if the parameter contains anything
                    if (string.IsNullOrEmpty(element.get_Parameter(p.Guid).AsString())) continue;
                    sbElemParameters.Append("    " + p.Keyword + " ");
                    sbElemParameters.Append(element.get_Parameter(p.Guid).AsString());
                }
                sbElemParameters.AppendLine();
            }
            return sbElemParameters;
        }
        #endregion
    }

    public class Filter
    {
        BuiltInParameter testParam; ParameterValueProvider pvp; FilterStringRuleEvaluator str;
        FilterStringRule paramFr; public ElementParameterFilter epf;

        public Filter(string valueQualifier, BuiltInParameter parameterName)
        {
            testParam = parameterName;
            pvp = new ParameterValueProvider(new ElementId((int)testParam));
            str = new FilterStringContains();
            paramFr = new FilterStringRule(pvp, str, valueQualifier, false);
            epf = new ElementParameterFilter(paramFr);
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
                    if (iv.UNITS_BORE_MM) testedDiameter = double.Parse(Conversion.PipeSizeToMm(((MEPCurve) element).Diameter/2));
                    else if (iv.UNITS_BORE_INCH) testedDiameter = double.Parse(Conversion.PipeSizeToInch(((MEPCurve) element).Diameter/2));

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

    public class Conversion
    {
        const double _inch_to_mm = 25.4;
        const double _foot_to_mm = 12 * _inch_to_mm;
        const double _foot_to_inch = 12;

        /// <summary>
        /// Return a string for a real number formatted to two decimal places.
        /// </summary>
        public static string RealString(double a)
        {
            //return a.ToString("0.##");
            return (Math.Truncate(a * 100) / 100).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"));
        }

        /// <summary>
        /// Return a string for an XYZ point or vector with its coordinates converted from feet to millimetres and formatted to two decimal places.
        /// </summary>
        public static string PointStringMm(XYZ p)
        {
            return string.Format("{0:0.00} {1:0.00} {2:0.00}",
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
            return string.Format("{0}", RealString(l*2*_foot_to_inch));
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

    public class EndWriter
    {
        public static StringBuilder WriteEP1 (Element element, Connector connector)
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
        private UIDocument _uiDoc;
        public Result CreateAllItemsSchedule(UIDocument uiDoc)
        {
            try
            {
                _uiDoc = uiDoc;
                Document doc = _uiDoc.Document;
                FilteredElementCollector sharedParameters = new FilteredElementCollector(doc);
                sharedParameters.OfClass(typeof (SharedParameterElement));

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
                        where param.GuidValue.CompareTo(pDef.Guid) == 0 select param).First();
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
                    SharedParameterElement parameter = (from SharedParameterElement param in sharedParameters where param.GuidValue.CompareTo(pDef.Guid) == 0
                                                        select param).First();
                    SchedulableField queryField = (from fld in schFields where fld.ParameterId.IntegerValue == parameter.Id.IntegerValue select fld).First();

                    ScheduleField field = schedFilter.Definition.AddField(queryField);
                    if (pDef.Name != "PCF_ELEM_TYPE") continue;
                    ScheduleFilter filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.HasParameter);
                    schedFilter.Definition.AddFilter(filter);
                    filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.NotEqual,"");
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
                                                        where param.GuidValue.CompareTo(pDef.Guid) == 0 select param).First();
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
}