using System;
using System.Collections.Generic;
using System.Linq;
//using MoreLinq;
using System.Data;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using dh = Shared.DataHandler;

namespace MEPUtils
{
    public class InsulationHandler
    {
        /// <summary>
        /// This method is used to set and save settings for insulation creation for Pipe Accessories (valves etc.)
        /// </summary>
        public Result ExecuteInsulationSettings(UIApplication uiApp)
        {
            InsulationSettingsWindow isw = new InsulationSettingsWindow(uiApp);
            isw.ShowDialog();
            isw.Close();
            using (Stream stream = new FileStream(isw.PathToSettingsXml, FileMode.Create, FileAccess.Write))
            {
                isw.Settings.WriteXml(stream);
            }

            return Result.Succeeded;
        }

        public static Result CreateAllInsulation(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            //Collect all the elements to insulate
            var pipes = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeCurves);
            var fittings = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting);
            var accessories = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory);

            //Filter out grouped items
            pipes = pipes.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();
            fittings = fittings.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();
            accessories = accessories.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();

            var insPar = GetInsulationParameters();
            var insSet = GetInsulationSettings(doc);

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create all insulation");

                //TODO: Split the InsulateElement into three methods for each kind -- I think it would make it more simple
                foreach (Element element in pipes) InsulatePipe(doc, element, insPar); //Works
                foreach (Element element in fittings) InsulateFitting(doc, element, insPar, insSet);
                foreach (Element element in accessories)
                {
                    Parameter insulationProjectedPar = element.LookupParameter("Insulation Projected");
                    if (insulationProjectedPar != null)
                    {
                        try
                        {
                            InsulateAccessoryWithDummyInsulation(doc, element, insPar, insSet, insulationProjectedPar);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Element {element.Id} failed at InsulateAccessoryWithDummyInsulation with following Exception: {e.Message}");
                        }
                    }
                    else InsulateAccessory(doc, element, insPar, insSet);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }

        private static DataTable GetInsulationSettings(Document doc)
        {
            //Manage Insulation creation settings
            //Test if settings file exist
            string pn = doc.ProjectInformation.Name;
            string pathToSettingsXml =
                Environment.ExpandEnvironmentVariables(
                    $"%AppData%\\MyRevitAddins\\MEPUtils\\Settings.{pn}.Insulation.xml"); //Magic text?
            bool settingsExist = File.Exists(pathToSettingsXml);

            //Initialize an empty datatable
            DataTable settings = new DataTable("InsulationSettings");

            if (settingsExist) //Read file if exists
            {
                using (Stream stream = new FileStream(pathToSettingsXml, FileMode.Open, FileAccess.Read))
                {
                    DataSet ds = new DataSet();
                    ds.ReadXml(stream);
                    settings = ds.Tables[0];
                }
            }
            else
                throw new Exception(
                    "Insulation creation settings file does not exist! Run configuration routine first!");
            return settings;
        }

        private static DataTable GetInsulationParameters()
        {
            //Manage Insulation parameters settings
            string pathToInsulationCsv =
                Environment.ExpandEnvironmentVariables("%AppData%\\MyRevitAddins\\MEPUtils\\Insulation.csv");
            bool fileExists = File.Exists(pathToInsulationCsv);
            if (!fileExists)
                throw new Exception(
                    @"No insulation configuration file exists at: %AppData%\MyRevitAddins\MEPUtils\Insulation.csv");

            //DataSet insulationDataSet = DataHandler.ImportExcelToDataSet(pathToInsulationExcel, "YES");
            //DataTable insulationData = DataHandler.ReadDataTable(insulationDataSet.Tables, "Insulation");
            //TODO: Interop is very slow. Implement a .csv solution.
            DataTable insulationData = CsvReader.ReadInsulationCsv(pathToInsulationCsv);
            return insulationData;
        }

        private static void InsulatePipe(Document doc, Element e, DataTable insPar)
        {
            #region Initialization
            //Read common configuration values
            string sysAbbr = e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();

            //Declare insulation thickness vars
            var dia = ((Pipe)e).Diameter.FtToMm().Round(0);
            double specifiedInsulationThickness = ReadThickness(sysAbbr, insPar, dia); //In feet already
            #endregion

            //Retrieve insulation type parameter and see if the pipe is already insulated
            Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                //Case: If the pipe is already insulated, check to see if insulation is correct
                Parameter parInsThickness = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                double existingInsulationThickness = parInsThickness.AsDouble(); //In feet

                //Test if existing thickness is as specified
                //If ok -> do nothing, if not -> fix it
                if (!specifiedInsulationThickness.Equalz(existingInsulationThickness, 1.0e-9))
                {
                    ElementId id = InsulationLiningBase.GetInsulationIds(doc, e.Id).FirstOrDefault();
                    if (id == null) return;
                    if (specifiedInsulationThickness.Equalz(0, Extensions._epx)) { doc.Delete(id); return; }
                    PipeInsulation insulation = doc.GetElement(id) as PipeInsulation;
                    if (insulation == null) return;
                    insulation.Thickness = specifiedInsulationThickness;
                }
            }
            else
            {
                //Case: If no insulation -> add insulation
                //Read pipeinsulation type and get the type
                string pipeInsulationName = dh.ReadParameterFromDataTable(sysAbbr, insPar, "Type");
                if (pipeInsulationName == null) return;
                PipeInsulationType pipeInsulationType =
                    fi.GetElements<PipeInsulationType, BuiltInParameter>(doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                //Test to see if the specified insulation is 0
                if (specifiedInsulationThickness.Equalz(0, Extensions._epx)) return;

                //Create insulation
                PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
            }

        }

        private static void InsulateFitting(Document doc, Element e, DataTable insPar, DataTable insSet)
        {
            #region Initialization
            //Read configuration data
            //var insPar = GetInsulationParameters();

            //Read common configuration values
            string sysAbbr = e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();

            //Declare insulation thickness vars
            #region Retrieve fitting diameter
            double dia;

            //See if the fittings is in the settings list, else return no action done
            if (insSet.AsEnumerable().Any(row => row.Field<string>("FamilyAndType")
                == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()))
            {
                //See if element is not allowed to be insulated
                var query = insSet.AsEnumerable()
                    .Where(row => row.Field<string>("FamilyAndType") == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                    .Select(row => row.Field<string>("AddInsulation"));
                bool insulationAllowed = bool.Parse(query.FirstOrDefault());


                #region DiameterRead
                //Retrieve specified insulation thickness
                var mf = ((FamilyInstance)e).MEPModel as MechanicalFitting;
                //Case: Reducer
                if (mf.PartType.ToString() == "Transition")
                {
                    //Retrieve connector dimensions
                    var cons = mp.GetConnectors(e);

                    //Insulate after the larger diameter
                    double primDia = (cons.Primary.Radius * 2).FtToMm().Round(0);
                    double secDia = (cons.Secondary.Radius * 2).FtToMm().Round(0);

                    dia = primDia > secDia ? primDia : secDia;
                }
                //Case: Other fitting
                else
                {
                    //Retrieve connector dimensions
                    var cons = mp.GetConnectors(e);
                    dia = (cons.Primary.Radius * 2).FtToMm().Round(0);
                }
                #endregion

                #endregion

                #region Read specified Insulation Thickness
                double specifiedInsulationThickness = ReadThickness(sysAbbr, insPar, dia); //In feet
                #endregion
                #endregion

                //Retrieve insulation type parameter and see if the fitting is already insulated
                Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
                if (parInsTypeCheck.HasValue)
                {
                    //Case Tee: If the element is a tee, delete any existing insulation
                    //Or it should not have insulation
                    if (mf.PartType.ToString() == "Tee" || insulationAllowed == false)
                    {
                        doc.Delete(InsulationLiningBase.GetInsulationIds(doc, e.Id));
                        if (mf.PartType.ToString() == "Tee") InsulateTee();
                        return;
                    }

                    //Case: If the fitting is already insulated, check to see if insulation is correct
                    Parameter parInsThickness = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                    double existingInsulationThickness = parInsThickness.AsDouble(); //In feet

                    //Test if existing thickness is as specified
                    //If ok -> do nothing, if not -> fix it
                    if (specifiedInsulationThickness.Equalz(existingInsulationThickness, 1.0e-9) == false)
                    {
                        //Case: specifiedInsulationThickness is 0
                        if (specifiedInsulationThickness.Equalz(0, 1.0e-9))
                        {
                            doc.Delete(InsulationLiningBase.GetInsulationIds(doc, e.Id));
                            return;
                        }

                        ElementId id = InsulationLiningBase.GetInsulationIds(doc, e.Id).FirstOrDefault();
                        if (id == null) return;
                        PipeInsulation insulation = doc.GetElement(id) as PipeInsulation;
                        if (insulation == null) return;
                        //Can cause exception if specifiedInsulation = 0
                        //This can happen if the PipingSystem Type Abbreviation does not exist in the
                        //Insulation.xlsx file and ReadThickness returns 0
                        //TODO: Write a general fix for this
                        insulation.Thickness = specifiedInsulationThickness;
                    }
                }
                else
                {
                    if (mf.PartType.ToString() == "Tee" && insulationAllowed == true)
                    {
                        InsulateTee();
                        return;
                    }

                    if (insulationAllowed && specifiedInsulationThickness > 1.0e-6) //Insulate only if insulation is allowed and insulation thickness is above 0
                    {
                        //Case: If no insulation -> add insulation
                        //Read pipeinsulation type and get the type
                        string pipeInsulationName = dh.ReadParameterFromDataTable(sysAbbr, insPar, "Type");
                        if (pipeInsulationName == null) return;
                        PipeInsulationType pipeInsulationType =
                            fi.GetElements<PipeInsulationType, BuiltInParameter>(doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                        if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                        //Create insulation
                        PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
                    }
                }

                //Local method to insulate Tees
                void InsulateTee()
                {
                    Parameter par1 = e.LookupParameter("Insulation Projected");
                    Parameter par2 = e.LookupParameter("Dummy Insulation Visible");

                    if (specifiedInsulationThickness.Equalz(0, Extensions._epx))
                    {
                        //Set insulation
                        if (par1 == null) return;
                        par1.Set(specifiedInsulationThickness);

                        //Make invisible if not
                        if (par2 == null) return;
                        if (par2.AsInteger() == 1) par2.Set(0);
                    }

                    //Set insulation
                    if (par1 == null) return;
                    par1.Set(specifiedInsulationThickness);

                    //Make visible if not
                    if (par2 == null) return;
                    if (par2.AsInteger() == 0) par2.Set(1);
                }
            }
        }

        private static void InsulateAccessory(Document doc, Element e, DataTable insPar, DataTable insSet)
        {
            #region Initialization

            //Read common configuration values
            string sysAbbr = e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();

            //Declare insulation thickness vars
            #region Retrieve accessory diameter
            var cons = mp.GetConnectors(e);
            double dia = (cons.Primary.Radius * 2).FtToMm().Round(0);
            #endregion

            #region Read specified Insulation Thickness

            double specifiedInsulationThickness;

            //This try/catch is introduced to catch exceptions where the specified diameter is not
            //listed in the insulation excel table
            try
            {
                specifiedInsulationThickness = ReadThickness(sysAbbr, insPar, dia); //In feet
            }
            catch (Exception)
            {
                //This is to handle non standard valves -- usually small bore stuff for air venting and alike
                specifiedInsulationThickness = 0;
            }

            #endregion
            #endregion

            //See if the accessory is in the settings list, else return no action done
            if (insSet.AsEnumerable().Any(row => row.Field<string>("FamilyAndType")
                == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()))
            {
                //See if element is not allowed to be insulated
                var query = insSet.AsEnumerable()
                    .Where(row => row.Field<string>("FamilyAndType") == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                    .Select(row => row.Field<string>("AddInsulation"));
                bool insulationAllowed = bool.Parse(query.FirstOrDefault());

                //Retrieve insulation type parameter and see if the accessory is already insulated
                Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
                if (parInsTypeCheck.HasValue)
                {
                    //If not allowed (false is read) negate the false to true to trigger the following if
                    //Delete any existing insulation and return
                    if (!insulationAllowed)
                    {
                        doc.Delete(InsulationLiningBase.GetInsulationIds(doc, e.Id));
                        return;
                    }

                    //Case: If the accessory is already insulated, check to see if insulation is correct
                    Parameter parInsThickness = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                    double existingInsulationThickness = parInsThickness.AsDouble(); //In feet

                    //Test if existing thickness is as specified
                    //If ok -> do nothing, if not -> fix it
                    if (!specifiedInsulationThickness.Equalz(existingInsulationThickness, 1.0e-9))
                    {
                        ElementId id = InsulationLiningBase.GetInsulationIds(doc, e.Id).FirstOrDefault();
                        if (id == null) return;
                        PipeInsulation insulation = doc.GetElement(id) as PipeInsulation;
                        if (insulation == null) return;
                        insulation.Thickness = specifiedInsulationThickness;
                    }
                }
                else
                {
                    //Case: If no insulation -> add insulation if allowed
                    if (!insulationAllowed) return;

                    //Read pipeinsulation type and get the type
                    string pipeInsulationName = dh.ReadParameterFromDataTable(sysAbbr, insPar, "Type");
                    if (pipeInsulationName == null) return;
                    PipeInsulationType pipeInsulationType =
                        fi.GetElements<PipeInsulationType, BuiltInParameter>(doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                    if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                    //Create insulation
                    PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
                }
            }
        }

        private static void InsulateAccessoryWithDummyInsulation(Document doc, Element e, DataTable insPar, DataTable insSet, Parameter insulationProjectedPar)
        {
            #region Initialization
            //Get the visibility parameter
            Parameter insulationVisibilityPar = e.LookupParameter("Dummy Insulation Visible");

            //Read common configuration values
            string sysAbbr = e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();

            //Declare insulation thickness vars
            #region Retrieve accessory diameter
            var cons = mp.GetConnectors(e);
            double dia = (cons.Primary.Radius * 2).FtToMm().Round(0);
            #endregion

            #region Delete any existing built-in insulation

            //Retrieve insulation type parameter and see if the accessory is already insulated
            Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                //If not allowed (false is read) negate the false to true to trigger the following if
                //Delete any existing insulation and return
                doc.Delete(InsulationLiningBase.GetInsulationIds(doc, e.Id));
            }

            #endregion

            #region Read specified Insulation Thickness

            double specifiedInsulationThickness;

            //This try/catch is introduced to catch exceptions where the specified diameter is not
            //listed in the insulation excel table
            try
            {
                specifiedInsulationThickness = ReadThickness(sysAbbr, insPar, dia); //In feet
            }
            catch (Exception)
            {
                throw new Exception($"Element {e.Id.ToString()} has diameter not listen in Insulation excel file!");
            }

            #endregion
            #endregion

            //See if the accessory is in the settings list, else return no action done
            if (insSet.AsEnumerable().Any(row => row.Field<string>("FamilyAndType")
                == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()))
            {
                //See if element is not allowed to be insulated
                var query = insSet.AsEnumerable()
                    .Where(row => row.Field<string>("FamilyAndType") == e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                    .Select(row => row.Field<string>("AddInsulation"));
                bool insulationAllowed = bool.Parse(query.FirstOrDefault());

                //Commense insulating if not and checking existing insulation for correctness
                double existingInsulationThickness = insulationProjectedPar.AsDouble();

                //Case: Insulation allowed
                if (insulationAllowed)
                {
                    if (existingInsulationThickness.Equalz(specifiedInsulationThickness, 1.0e-6))
                    {
                        //Case: Existing insulation thickness equals specified
                        if (insulationVisibilityPar.AsInteger() == 0 && specifiedInsulationThickness > 1.0e-6) insulationVisibilityPar.Set(1);
                        else if (insulationVisibilityPar.AsInteger() == 1 && specifiedInsulationThickness < 1.0e-6) insulationVisibilityPar.Set(0);
                    }
                    else
                    {
                        //Case: Existing insulation does not equal specified
                        insulationProjectedPar.Set(specifiedInsulationThickness);
                        //Subcase: Specified insulation is 0
                        if (specifiedInsulationThickness.Equalz(0, 1.0e-6))
                        {
                            if (insulationVisibilityPar.AsInteger() == 1) insulationVisibilityPar.Set(0);
                        }
                    }
                }
                else
                //Case: Insulation disallowed
                {
                    if (existingInsulationThickness > 1.0e-6)
                    {
                        insulationProjectedPar.Set(0);
                    }

                    if (insulationVisibilityPar.AsInteger() == 1) insulationVisibilityPar.Set(0);
                }
            }
        }


        private static double ReadThickness(string sysAbbr, DataTable insPar, double dia)
        {
            string insThicknessAsReadFromDataTable = dh.ReadParameterFromDataTable(sysAbbr, insPar, dia.ToString());
            if (insThicknessAsReadFromDataTable == null) return 0;
            return double.Parse(insThicknessAsReadFromDataTable).Round(0).MmToFt();
        }

        public static Result DeleteAllPipeInsulation(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            var allInsulation = fi.GetElements<PipeInsulation, BuiltInParameter>(doc, BuiltInParameter.INVALID);
            if (allInsulation == null) return Result.Failed;
            else if (allInsulation.Count == 0) return Result.Failed;

            var fittings = fi.GetElements<FamilyInstance, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting).ToHashSet();



            Transaction tx = new Transaction(doc);
            tx.Start("Delete all insulation!");
            foreach (Element el in allInsulation) doc.Delete(el.Id);

            foreach (FamilyInstance fi in fittings)
            {
                var mf = fi.MEPModel as MechanicalFitting;
                if (mf.PartType.ToString() == "Tee")
                {
                    //Set insulation to 0
                    Parameter par1 = fi.LookupParameter("Insulation Projected");
                    par1?.Set(0);

                    //Make invisible also
                    Parameter par2 = fi.LookupParameter("Dummy Insulation Visible");
                    if (par2.AsInteger() == 1) par2.Set(0);
                }
            }

            tx.Commit();

            return Result.Succeeded;
        }
    }
}
