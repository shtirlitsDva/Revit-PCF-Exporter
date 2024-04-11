using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Text;

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
                if (e.Category.Id.IntegerValue == (int)PACat)
                {
                    if (e.LookupParameter("Insulation Projected") == null) new IWPipeAccessoryGeneral(e);
                    else return new IWPipeAccessoryCustom(e);
                }
                if (e.Category.Id.IntegerValue == (int)FitCat)
                {
                    FamilyInstance fi = e as FamilyInstance;
                    var mf = ((FamilyInstance)e).MEPModel as MechanicalFitting;

                    if (mf.PartType == PartType.Transition) return new IWTransition(e);

                    if (mf.PartType == PartType.Tee)
                    {
                        if (fi.LookupParameter("Insulation Projected") == null) return new IWTeeGeneral(e);
                        else return new IWTeeCustom(e);
                    }

                    return new IWFittingGeneral(e);
                }
            }
            return null;
        }
    }
    public abstract class InsulationWrapper : IW
    {
        public string sysAbbr
        {
            get
            {
                Parameter par = e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM);
                if (par == null) return "";
                return par.AsString();
            }
        }

        private Element e;
        public InsulationWrapper(Element e)
        {
            this.e = e;
        }
    }
    public class IWPipe : InsulationWrapper
    {
        public IWPipe(Element e) : base(e)
        {
        }
    }
    public class IWPipeAccessoryGeneral : InsulationWrapper
    {
        public IWPipeAccessoryGeneral(Element e) : base(e)
        {
        }
    }
    public class IWPipeAccessoryCustom : InsulationWrapper
    {
        public IWPipeAccessoryCustom(Element e) : base(e)
        {
        }
    }
    public class IWTeeGeneral : InsulationWrapper
    {
        public IWTeeGeneral(Element e) : base(e)
        {
        }
    }
    public class IWTeeCustom : InsulationWrapper
    {
        public IWTeeCustom(Element e) : base(e)
        {
        }
    }
    public class IWTransition : InsulationWrapper
    {
        public IWTransition(Element e) : base(e)
        {
        }
    }
    public class IWFittingGeneral : InsulationWrapper
    {
        public IWFittingGeneral(Element e) : base(e)
        {
        }
    }
}
