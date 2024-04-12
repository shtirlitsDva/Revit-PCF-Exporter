using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.IO;

using Shared;
using fi = Shared.Filter;
using mp = Shared.MepUtils;
using dh = Shared.DataHandler;
using System.Linq;
using System.Diagnostics;

namespace MEPUtils.InsulationHandler
{
    public static class Settings
    {
        public static DataTable InsulationParameters { get; set; }
        public static DataTable InsulationSettings { get; set; }
    }
    public interface IW
    {
        string sysAbbr { get; }
        void Insulate(Document doc);
    }
    public static class IWFactory   
    {
        private static BuiltInCategory PACat = BuiltInCategory.OST_PipeAccessory;
        private static BuiltInCategory FitCat = BuiltInCategory.OST_PipeFitting;
        public static IW CreateIW(Element e)
        {
            if (e is Pipe) return new IWPipe(e);
            if (e is FamilyInstance)
            {
                var id = e.Id.ToString();
                ;
                if (e.Category.Id.IntegerValue == (int)PACat)
                {
                    if (e.LookupParameter("Insulation Projected") == null) return new IWFamilyInstanceGeneral(e);
                    else return new IWFamilyInstanceCustom(e);
                }
                if (e.Category.Id.IntegerValue == (int)FitCat)
                {
                    FamilyInstance fi = e as FamilyInstance;
                    var mf = ((FamilyInstance)e).MEPModel as MechanicalFitting;

                    if (mf.PartType == PartType.Transition) return new IWTransition(e);

                    if (mf.PartType == PartType.Tee)
                    {
                        if (fi.LookupParameter("Insulation Projected") == null) return new IWFamilyInstanceGeneral(e);
                        else return new IWFamilyInstanceCustom(e);
                    }

                    return new IWFamilyInstanceGeneral(e);
                }
            }

            string path = 
                Environment.ExpandEnvironmentVariables("%temp%") + "\\" + "errorElId.txt";
            File.WriteAllText(path, e.Id.ToString());
            Process.Start("notepad.exe", path);

            throw new Exception($"Element {e.Id} is not a pipe, pipe accessory, or fitting!");
        }
    }
    public abstract class InsulationWrapper : IW
    {
        public string sysAbbr { get; }
        protected string famAndType => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();
        protected bool insulationAllowed { get; set; }
        protected readonly Element e;
        public InsulationWrapper(Element element)
        {
            this.e = element;
            sysAbbr = this.e.get_Parameter(
                BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM)?.AsString() ?? "";

            var query = Settings.InsulationSettings.AsEnumerable()
            .Where(row => row.Field<string>("FamilyAndType") == famAndType)
            .Select(row => row.Field<string>("AddInsulation")).FirstOrDefault();

            //this to set the insulationAllowed property to false if query is null
            insulationAllowed = bool.TryParse(query, out bool temp) ? temp : false;
        }

        public abstract void Insulate(Document doc);
        protected double ReadThickness(double dia)
        {
            string insThicknessAsReadFromDataTable = dh.ReadParameterFromDataTable(
                sysAbbr, Settings.InsulationParameters, dia.ToString());
            if (insThicknessAsReadFromDataTable == null) return 0;
            return double.Parse(insThicknessAsReadFromDataTable).Round(0).MmToFt();
        }
    }
    public class IWPipe : InsulationWrapper
    {
        public IWPipe(Element e) : base(e) { }

        public override void Insulate(Document doc)
        {
            //Declare insulation thickness vars
            var dia = ((Pipe)e).Diameter.FtToMm().Round(0);
            double specifiedInsulationThickness = ReadThickness(dia); //In feet already

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
                string pipeInsulationName = dh.ReadParameterFromDataTable(
                    sysAbbr, Settings.InsulationParameters, "Type");
                if (pipeInsulationName == null) return;
                PipeInsulationType pipeInsulationType =
                    fi.GetElements<PipeInsulationType, BuiltInParameter>(
                        doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                //Test to see if the specified insulation is 0
                if (specifiedInsulationThickness.Equalz(0, Extensions._epx)) return;

                //Create insulation
                PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
            }
        }
    }
    public class IWFamilyInstanceGeneral : InsulationWrapper
    {
        public IWFamilyInstanceGeneral(Element e) : base(e) { }
        public override void Insulate(Document doc)
        {
            var cons = mp.GetConnectors(e);
            double dia = (cons.Primary.Radius * 2).FtToMm().Round(0);

            double specifiedInsulationThickness = ReadThickness(dia);

            //Logic for adding insulation
            //Retrieve insulation type parameter and see if the accessory is already insulated
            Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                //If not allowed (false is read) negate the false to true to trigger the following if
                //Delete any existing insulation and return
                if (!insulationAllowed || specifiedInsulationThickness == 0)
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
                if (!insulationAllowed || specifiedInsulationThickness == 0) return;

                //Read pipeinsulation type and get the type
                string pipeInsulationName = dh.ReadParameterFromDataTable(
                    sysAbbr, Settings.InsulationParameters, "Type");
                if (pipeInsulationName == null) return;
                PipeInsulationType pipeInsulationType =
                    fi.GetElements<PipeInsulationType, BuiltInParameter>(
                        doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                //Create insulation
                PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
            }
        }
    }
    public class IWFamilyInstanceCustom : InsulationWrapper
    {
        public IWFamilyInstanceCustom(Element e) : base(e) { }

        public override void Insulate(Document doc)
        {
            Parameter insulationProjectedPar =
                e.LookupParameter("Insulation Projected"); //existence should be guaranteed by factory
            Parameter insulationVisibilityPar = e.LookupParameter("Dummy Insulation Visible");
            if (insulationVisibilityPar == null)
                throw new Exception($"Insulation visibility parameter (Dummy Insulation Visible) not found for element {e.Id}!");

            //Retrieve insulation type parameter and see if the accessory is already insulated
            Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
                //Delete any existing insulation and return
                doc.Delete(InsulationLiningBase.GetInsulationIds(doc, e.Id));

            //Add insulation
            var cons = mp.GetConnectors(e);
            double dia = (cons.Primary.Radius * 2).FtToMm().Round(0);

            double specifiedInsulationThickness = ReadThickness(dia);
            if (specifiedInsulationThickness.Equalz(0, 1.0e-6)) insulationAllowed = false;
            double existingInsulationThickness = insulationProjectedPar.AsDouble();

            if (insulationAllowed)
            {
                //Turn on insulation visibility if insulation is allowed
                insulationVisibilityPar.Set(1);

                //Case: Existing insulation does not equal specified
                if (!existingInsulationThickness.Equalz(specifiedInsulationThickness, 1.0e-6))
                    insulationProjectedPar.Set(specifiedInsulationThickness);
            }
            else
            {
                //Turn off insulation visibility if insulation is not allowed
                insulationVisibilityPar.Set(0);
                insulationProjectedPar.Set(0);
            }
        }
    }
    public class IWTransition : InsulationWrapper
    {
        public IWTransition(Element e) : base(e) {}
        public override void Insulate(Document doc)
        {
            //Add insulation
            var cons = mp.GetConnectors(e);

            //Insulate after the larger diameter
            double primDia = (cons.Primary.Radius * 2).FtToMm().Round(0);
            double secDia = (cons.Secondary.Radius * 2).FtToMm().Round(0);

            double dia = primDia > secDia ? primDia : secDia;

            double specifiedInsulationThickness = ReadThickness(dia);
            if (specifiedInsulationThickness.Equalz(0, 1.0e-6)) insulationAllowed = false;

            //Logic for adding insulation
            //Retrieve insulation type parameter and see if the accessory is already insulated
            Parameter parInsTypeCheck = e.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                //If not allowed (false is read) negate the false to true to trigger the following if
                //Delete any existing insulation and return
                if (!insulationAllowed || specifiedInsulationThickness == 0)
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
                if (!insulationAllowed || specifiedInsulationThickness == 0) return;

                //Read pipeinsulation type and get the type
                string pipeInsulationName = dh.ReadParameterFromDataTable(
                    sysAbbr, Settings.InsulationParameters, "Type");
                if (pipeInsulationName == null) return;
                PipeInsulationType pipeInsulationType =
                    fi.GetElements<PipeInsulationType, BuiltInParameter>(
                        doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                if (pipeInsulationType == null) throw new Exception($"No pipe insulation type named {pipeInsulationName}!");

                //Create insulation
                PipeInsulation.Create(doc, e.Id, pipeInsulationType.Id, specifiedInsulationThickness);
            }
        }
    }
}
