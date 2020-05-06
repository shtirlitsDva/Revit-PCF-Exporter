using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public static string FullFileName;
        public static string ExcelSheetElements = "Elements";
        public static string ExcelSheetPipelines = "Pipelines";

        //PCF PROJECT-IDENTIFIER value setting
        public static string PCF_PROJECT_IDENTIFIER = "";

        //Execution control
        public static bool ExportAllOneFile = true;
        public static bool ExportAllSepFiles = false;
        public static bool ExportSpecificPipeLine = false;
        public static bool ExportSelection = false;
        public static bool WriteWallThickness = false;
        public static bool ExportToIsogen = false;
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

        public static double DiameterLimit = 0;
        public static string PCF_ELEM_SPEC_FILTER = "EXISTING";

        #endregion Filters

        #region Element parameter definition
        //Shared parameter group
        //public const string PCF_GROUP_NAME = "PCF"; OBSOLETE
        public const BuiltInParameterGroup PCF_BUILTIN_GROUP_NAME = BuiltInParameterGroup.PG_ANALYTICAL_MODEL;
        #endregion
    }

    public class Composer
    {
        #region Preamble
        //PCF Preamble composition

        public StringBuilder PreambleComposer()
        {
            StringBuilder sbPreamble = new StringBuilder();
            sbPreamble.Append("ISOGEN-FILES ISOGEN.FLS");
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

        public static StringBuilder CIIWriter(Document doc, string systemAbbreviation)
        {
            StringBuilder sbCII = new StringBuilder();
            //Handle CII export parameters
            //Instantiate collector
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            //Get the elements
            PipingSystemType sQuery = collector.OfClass(typeof(PipingSystemType))
                .WherePasses(Shared.Filter.ParameterValueGenericFilter(doc, systemAbbreviation, BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM))
                .Cast<PipingSystemType>()
                .FirstOrDefault();

            ////Select correct systemType
            //PipingSystemType sQuery = (from PipingSystemType st in collector
            //                           where string.Equals(st.Abbreviation, systemAbbreviation)
            //                           select st).FirstOrDefault();

            var query = from p in new plst().LPAll
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
            //if (string.Equals(pm.AsString(), "EXISTING-INCLUDE")) return new StringBuilder();

            //Write ITEM-CODE et al
            StringBuilder sb = new StringBuilder();

            pdef matId = new plst().PCF_MAT_ID;
            pdef matDescr = new plst().PCF_MAT_DESCR;

            string itemCode = element.get_Parameter(matId.Guid).AsString();
            string itemDescr = element.get_Parameter(matDescr.Guid).AsString();
            string key = Shared.MepUtils.GetElementPipingSystemType(element, doc).Abbreviation;

            sb.AppendLine("    ITEM-CODE " + key + "-" + itemCode);
            sb.AppendLine("    ITEM-DESCRIPTION " + itemDescr);

            return sb;
        }

        #endregion

        #region ELEM parameter writer
        public StringBuilder ElemParameterWriter(Element passedElement)
        {
            StringBuilder sbElemParameters = new StringBuilder();

            var pQuery = from p in new plst().LPAll
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

    public static class Filters
    {
        /// <summary>
        /// Tests the diameter of the pipe or primary connector of element against the diameter limit set in the interface.
        /// </summary>
        /// <param name="passedElement"></param>
        /// <returns>True if diameter is larger than limit and false if smaller.</returns>
        public static bool FilterDL(Element element)
        {
            double diameterLimit = iv.DiameterLimit;
            double testedDiameter = 0;
            switch (element)
            {
                case MEPCurve pipe:
                    if (iv.UNITS_BORE_MM) testedDiameter = pipe.Diameter.FtToMm().Round();
                    else if (iv.UNITS_BORE_INCH) testedDiameter = pipe.Diameter.FtToInch().Round();
                    break;
                case FamilyInstance fi:
                    //Declare a variable for 
                    Connector testedConnector = null;
                    //Gather connectors of the element
                    var cons = Shared.MepUtils.GetConnectors(element);
                    //This check is more robust than NTR_Functions one, must keep this part when merging
                    if (cons.Primary == null) break;
                    else if (cons.Count == 0) break;
                    else if (cons.Count == 1 || cons.Count > 2) testedConnector = cons.Primary;
                    else if (cons.Count == 2) testedConnector = cons.Largest ?? cons.Primary; //Largest is only defined for reducers

                    if (iv.UNITS_BORE_MM) testedDiameter = (testedConnector.Radius * 2).FtToMm().Round();
                    else if (iv.UNITS_BORE_INCH) testedDiameter = (testedConnector.Radius * 2).FtToInch().Round(3);
                    break;
            }
            return testedDiameter >= diameterLimit;
        }

        public static bool PipingSystemAllowed(this Element elem, Document doc)
        {
            Element pipingSystemType = doc.GetElement(elem.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId());
            if (pipingSystemType == null) return false;
            Parameter pipingExclParameter = pipingSystemType?.get_Parameter(new Guid("c1c2c9fe-2634-42ba-89d0-5af699f54d4c"));
            if (pipingExclParameter == null) throw new Exception("PipingSystemAllowed cannot acces PCF_PIPL_EXCL! Does the parameter exist in the project?");
            if (pipingExclParameter.AsInteger() == 0) return true;
            else return false;
        }
    }

    public static class EndWriter
    {
        internal static string PointStringMm(XYZ p)
        {
            return string.Concat(
                Math.Round(p.X.FtToMm(), 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Y.FtToMm(), 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Z.FtToMm(), 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.GetCultureInfo("en-GB")));
        }

        public static StringBuilder WriteEP(XYZ endPoint)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(endPoint));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(endPoint));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteEP1(Element element, Connector connector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ connectorOrigin = connector.Origin;
            double connectorSize = connector.Radius;
            sbEndWriter.Append("    END-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(connectorOrigin));
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
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(connectorOrigin));
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
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(connectorOrigin));
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
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(connectorOrigin));
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
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(connectorOrigin));
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
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(elementLocation));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(elementLocation));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCP(XYZ point)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    CENTRE-POINT ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(point));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(point));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(XYZ point)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(point));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(point));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(FamilyInstance familyInstance, Connector passedConnector)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ elementLocation = ((LocationPoint)familyInstance.Location).Point;
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(elementLocation));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(elementLocation));
            double connectorSize = passedConnector.Radius;
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(Connector passedConnector, string SupplementaryAttributes)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(passedConnector.Origin));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(passedConnector.Origin));
            double connectorSize = passedConnector.Radius;
            sbEndWriter.Append(" ");
            if (InputVars.UNITS_BORE_MM) sbEndWriter.Append(Conversion.PipeSizeToMm(connectorSize));
            if (InputVars.UNITS_BORE_INCH) sbEndWriter.Append(Conversion.PipeSizeToInch(connectorSize));
            sbEndWriter.Append(" ");
            sbEndWriter.Append(SupplementaryAttributes);
            sbEndWriter.AppendLine();
            return sbEndWriter;
        }

        public static StringBuilder WriteCO(FamilyInstance familyInstance)
        {
            StringBuilder sbEndWriter = new StringBuilder();
            XYZ elementLocation = ((LocationPoint)familyInstance.Location).Point;
            sbEndWriter.Append("    CO-ORDS ");
            if (InputVars.UNITS_CO_ORDS_MM) sbEndWriter.Append(PointStringMm(elementLocation));
            if (InputVars.UNITS_CO_ORDS_INCH) sbEndWriter.Append(Conversion.PointStringInch(elementLocation));
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

                //curDomain = "PIPL";
                List<pdef> parList = new List<pdef>();
                plst Plst = new plst();
                parList.Add(Plst.PCF_PIPL_LINEID);
                parList.Add(Plst.PCF_PIPL_NOMCLASS);
                parList.Add(Plst.PCF_PIPL_TEMP);
                parList.Add(Plst.PCF_PIPL_AREA);
                parList.Add(Plst.PCF_PIPL_PROJID);
                parList.Add(Plst.PCF_PIPL_DATE);
                parList.Add(Plst.PCF_PIPL_DWGNAME);
                parList.Add(Plst.PCF_PIPL_REV);
                parList.Add(Plst.PCF_PIPL_TEGN);
                parList.Add(Plst.PCF_PIPL_KONTR);
                parList.Add(Plst.PCF_PIPL_GODK);
                foreach (pdef pDef in parList)
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
                BuildingCoderUtilities.InfoMsg(e.Message);
                return Result.Failed;
            }



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
                ConnectorSet connectorSet = Shared.MepUtils.GetConnectorManager(element).Connectors;

                Connector c1 = null;

                switch (element)
                {
                    case Pipe pipe:
                        //Filter out non-end types of connectors
                        c1 = (from Connector connector in connectorSet
                              where connector.ConnectorType.ToString().Equals("End")
                              select connector).FirstOrDefault();
                        break;
                    case FamilyInstance fi:
                        c1 = (from Connector connector in connectorSet
                              where connector.GetMEPConnectorInfo().IsPrimary
                              select connector).FirstOrDefault();
                        Connector c2 = (from Connector connector in connectorSet
                                        where connector.GetMEPConnectorInfo().IsSecondary
                                        select connector).FirstOrDefault();
                        if (c2 != null) if (c1.Radius > c2.Radius) c1 = c2;
                        break;
                    default:
                        break;
                }
                if (element is Pipe)
                {

                }

                if (element is FamilyInstance)
                {

                }

                string data = string.Empty;
                string source = Conversion.PipeSizeToMm(c1.Radius);
                int dia = Convert.ToInt32(source);
                pipeWallThk.TryGetValue(dia, out data);
                wallThkParameter.Set(data);
            }
        }

        internal static string ParameterValue(string keyword, string[] parNames, Element element)
        {
            List<string> values = new List<string>();
            foreach (string name in parNames)
            {
                string value = element.LookupParameter(name)?.ToValueString();
                if (!value.IsNullOrEmpty()) values.Add(value);
            }
            if (values.Count < 1) return "";
            else return $"    {keyword} {string.Join("_", values)}\n";
        }
    }

    public class BrokenPipesGroup
    {
        Element SeedElement;
        public List<Element> BrokenPipes = new List<Element>();
        public Element HealedPipe { get; set; } = null;
        public List<Element> SupportsOnPipe = new List<Element>();
        List<Connector> AllConnectors = new List<Connector>();
        readonly string CurSysAbr;

        public BrokenPipesGroup(Element seedElement, string sysAbr)
        {
            SeedElement = seedElement;
            SupportsOnPipe.Add(seedElement);
            CurSysAbr = sysAbr;
        }

        //Choose one and traverse in both directions finding other supports on same pipe
        //Continue conditions:
        //  1. Element is Pipe -> add to brokenPipesList, continue
        //      a. AND PipingSystemAbbreviation remains unchanged
        //      b. AND PCF_ELEM_SPEC remains unchanged
        //  2. Element is PipeAccessory and is one of the Support family instances -> add to supports on pipe
        //Break conditions:
        //  1. Element is PipeFitting -> Break
        //  2. Element is PipeAccessory and NOT an instance of a Support family -> Break
        //  3. Element is Pipe AND PipingSystemAbbreviation changes -> Break
        //  4. Element is Pipe AND PCF_ELEM_SPEC changes -> Break
        //  5. Free end -> Break

        public void Traverse(Document doc)
        {
            //Get connectors from the Seed Element
            Cons cons = MepUtils.GetConnectors(SeedElement);
            //Assign the connectors from the support to the two directions
            Connector firstSideCon = cons.Primary; Connector secondSideCon = cons.Secondary;

            #region SpecFinder
            //The spec of the support can be different from the pipe's
            //It is decided that the spec of the pipe is decisive
            //This means spec must be determined before loop starts
            //ATTENTION: If pipes on both sides have different spec -> no traversal needed -> the support is placed at a natural boundary

            Connector refFirstCon = null; Connector refSecondCon = null;

            if (firstSideCon.IsConnected)
            {
                var refFirstCons = MepUtils.GetAllConnectorsFromConnectorSet(firstSideCon.AllRefs);
                refFirstCon = refFirstCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            }
            else refFirstCon = DetectUnconnectedConnector(doc, firstSideCon);

            if (secondSideCon.IsConnected)
            {
                var refSecondCons = MepUtils.GetAllConnectorsFromConnectorSet(secondSideCon.AllRefs);
                refSecondCon = refSecondCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            }
            else refSecondCon = DetectUnconnectedConnector(doc, secondSideCon);

            string firstSpec = ""; string secondSpec = ""; string spec = "";

            if (refFirstCon != null)
            {
                Element el = refFirstCon.Owner;
                firstSpec = el.get_Parameter(new plst().PCF_ELEM_SPEC.Guid).AsString();
            }
            if (refSecondCon != null)
            {
                Element el = refSecondCon.Owner;
                secondSpec = el.get_Parameter(new plst().PCF_ELEM_SPEC.Guid).AsString();
            }

            if (firstSpec.IsNullOrEmpty() && secondSpec.IsNullOrEmpty()) return; //<- Both empty
            if (!firstSpec.IsNullOrEmpty() && secondSpec.IsNullOrEmpty()) spec = firstSpec; //<- First not empty, but second
            else if (firstSpec.IsNullOrEmpty() && !secondSpec.IsNullOrEmpty()) spec = secondSpec; //<- Second not empty, but first
            else
            {
                if (firstSpec == secondSpec) spec = firstSpec;
                else return; //<- Different specs -> support is at natural boundary
            }
            #endregion

            //Loop controller
            bool Continue = true;
            //Side controller
            bool firstSideDone = false;
            //Loop variables
            Connector start = null;
            //Initialize first loop
            start = firstSideCon;
            //Loop guard
            int i = 0;

            while (Continue)
            {
                //Loop guard, if too many iterations something is wrong
                i++;
                if (i > 10000) throw new Exception("Traverse loop in BrokenPipes has reached 10000 iterations -> something is wrong! \n" +
                                                   "Do you really have 10000 pipe pieces?");

                //Using a seed connector, "start", get the next element
                //If "start" does not yield a connector to continue on -> stop this side
                //Determine if next element is eligible for continue
                //If yes, add element to collections, get next connector
                //If not, continue next side if side not already done

                Connector refCon;

                if (start.IsConnected)
                {
                    var refCons = MepUtils.GetAllConnectorsFromConnectorSet(start.AllRefs); //<- DOES ALLREFS RETURN NULL IF EMPTY???
                    refCon = refCons.Where(x => x.Owner.IsType<Pipe>() || x.Owner.IsType<FamilyInstance>()).FirstOrDefault();
                }
                else refCon = DetectUnconnectedConnector(doc, start);

                //Break condition 5: Free end
                if (refCon == null) //Dead end
                {
                    if (firstSideDone == false)
                    {
                        //Dead end -> first side done -> continue second side
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; } //Dead end -> both sides done -> end traversal loop
                }

                Element elementToConsider = refCon.Owner;

                //Determine if the element is a support
                bool isSupport = elementToConsider.ComponentClass1(doc) == "Pipe Support";

                //Continuation 1a
                string elementSysAbr = elementToConsider.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();
                if (CurSysAbr != elementSysAbr)
                {
                    if (firstSideDone == false)
                    {
                        //Dead end -> first side done -> continue second side
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; } //Dead end -> both sides done -> end traversal
                }

                //Continuation 1b
                string elementSpec = elementToConsider.get_Parameter(new plst().PCF_ELEM_SPEC.Guid).AsString();
                if (spec != elementSpec && !isSupport) //The spec can be different for another support on the pipe, so it must accept those
                {
                    if (firstSideDone == false)
                    {
                        //Dead end -> first side done -> continue second side
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; } //Dead end -> both sides done -> end traversal
                }

                switch (elementToConsider)
                {
                    //Remove from pipeList, add to brokenPipesList, continue
                    case Pipe pipe:
                        BrokenPipes.Add(elementToConsider);
                        start = (from Connector c in pipe.ConnectorManager.Connectors //Find next seed connector
                                 where c.Id != refCon.Id && (int)c.ConnectorType == 1
                                 select c).FirstOrDefault();
                        break;
                    case FamilyInstance fi:
                        //Break condition 1: Element is a fitting
                        if (elementToConsider.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                        {
                            if (firstSideDone == false)
                            {
                                //Dead end -> first side done -> continue second side
                                firstSideDone = true; start = secondSideCon; continue;
                            }
                            else { Continue = false; break; } //Dead end -> both sides done -> end traversal
                        }
                        //Break condition 2: Element is a PipeAccessory and NOT a support
                        if (elementToConsider.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory
                            && !isSupport)
                        {
                            if (firstSideDone == false)
                            {
                                //Dead end -> first side done -> continue second side
                                firstSideDone = true; start = secondSideCon; continue;
                            }
                            else { Continue = false; break; } //Dead end -> both sides done -> end traversal
                        }
                        //If execution reaches this part, then the element is a support and is eligible for consideration
                        SupportsOnPipe.Add(elementToConsider);
                        //Find next seed connector
                        Cons supportCons = MepUtils.GetConnectors(elementToConsider);
                        if (refCon.GetMEPConnectorInfo().IsPrimary)
                        {
                            start = supportCons.Secondary;
                        }
                        else start = supportCons.Primary;

                        break;
                    default:
                        break;
                }
            }
        }

        public void CreateHealedPipe()
        {

        }

        private Connector DetectUnconnectedConnector(Document doc, Connector knownCon)
        {
            var allCons = MepUtils.GetALLConnectorsInDocument(doc);
            return allCons.Where(c => c.Equalz(knownCon, 0.00328) && c.Owner.Id.IntegerValue != knownCon.Owner.Id.IntegerValue).FirstOrDefault();
        }
    }
}