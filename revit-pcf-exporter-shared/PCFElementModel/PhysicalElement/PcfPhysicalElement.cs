using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;

using PCF_Functions;
using PCF_Exporter;

using plst = PCF_Functions.Parameters;
using pdef = PCF_Functions.ParameterDefinition;
using System.Linq;
using Shared;
using mp = Shared.MepUtils;

namespace PCF_Model
{
    public abstract class PcfPhysicalElement : IPcfElement
    {
        protected static Document doc => DocumentManager.Instance.Doc;
        public Element Element { get; }
        public ElementId ElementId => Element.Id;
        public HashSet<Connector> AllConectors => getAllConnectors();

        public string SystemAbbreviation => 
            Element.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsValueString();

        protected Cons Cons;
        protected static Dictionary<ElementId, FamilyInstance> SpindleDict = 
            new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_GenericModel)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Where(x => x.FamilyAndTypeName() == "Spindle direction: Spindle direction")
            .ToDictionary(x => x.SuperComponent.Id, x => x);
        public PcfPhysicalElement(Element element) { 
            Element = element; Cons = new Cons(Element); }
        public string GetParameterValue(pdef pdef) => pdef.GetValue(Element);
        public object GetParameterValue(string name)
        {
            Parameter par = Element.LookupParameter(name);
            if (par == null) return null;
            StorageType sT = par.StorageType;
            switch (sT)
            {
                case StorageType.None:
                    return null;
                case StorageType.Integer:
                    return par.AsInteger();
                case StorageType.Double:
                    return par.AsDouble();
                case StorageType.String:
                    return par.AsString();
                case StorageType.ElementId:
                    return par.AsElementId();
                default:
                    return null;
            }
        }
        public void SetParameterValue(pdef pdef, string value) => pdef.SetValue(Element, value);
        private HashSet<Connector> getAllConnectors() => mp.GetALLConnectorsFromElements(Element);
        #region Writing to string
        public StringBuilder ToPCFString()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine(plst.PCF_ELEM_TYPE.GetValue(Element));

                var Specification = plst.PCF_ELEM_SPEC.GetValue(Element);
                if (Specification == "EXISTING-INCLUDE")
                {
                    sb.AppendLine("    STATUS DOTTED-UNDIMENSIONED");
                    sb.AppendLine("    MATERIAL-LIST EXCLUDE");
                }

                sb.Append(WriteSpecificData());

                sb.Append(ParameterDataWriter.ParameterValue(
                    "TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, Element));

                sb.Append(WritePcfElemParameters());

                sb.Append(WriteSpindle());

                sb.Append(WriteTaps());

                sb.AppendLine($"    UCI {Element.UniqueId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in {this.GetType().Name} for element {Element.Id}\n" + ex.ToString());
            }

            return sb;
        }
        protected abstract StringBuilder WriteSpecificData();
        #endregion
        private StringBuilder WritePcfElemParameters()
        {
            var pQuery = plst.LPAll()
                .Where(p => p.Keyword.IsNotNoE() && p.Domain == ParameterDomain.ELEM);

            StringBuilder sb = new StringBuilder();

            foreach (pdef p in pQuery)
            {
                if (Element.get_Parameter(p.Guid) == null) continue;
                StorageType storageType = Element.get_Parameter(p.Guid).StorageType;
                //We only use strings to store PCF data
                switch (storageType)
                {
                    case StorageType.String:
                        string value = Element.get_Parameter(p.Guid).AsString();
                        if (value.IsNoE()) continue;
                        sb.AppendLine($"    {p.Keyword} {Element.get_Parameter(p.Guid).AsString()}");
                        break;
                    default:
                        throw new Exception($"Unsupported storage type!\n" + 
                            $"Element {Element.Id} for parameter {p.Name} returned\n" + 
                            $"unsupported storage type: {storageType}");
                }
            }

            return sb;
        }
        private StringBuilder WriteTaps()
        {
            StringBuilder sb = new StringBuilder();

            if (plst.PCF_ELEM_TAP1.GetValue(Element).IsNotNoE())
            {
                sb.Append(
                    PCF_Taps.TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP1", doc));
            }
            if (plst.PCF_ELEM_TAP2.GetValue(Element).IsNotNoE())
            {
                sb.Append(
                    PCF_Taps.TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP2", doc));
            }
            if (plst.PCF_ELEM_TAP3.GetValue(Element).IsNotNoE())
            {
                sb.Append(
                    PCF_Taps.TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP3", doc));
            }

            //Write the TAP taps
            if (plst.PCF_ELEM_TAPS.GetValue(Element).IsNotNoE())
            {
                string raw = plst.PCF_ELEM_TAPS.GetValue(Element);
                var contents = raw.Split(';').Select(x => x.ToLower()).ToList();
                foreach (string uci in contents)
                {
                    Element tap = doc.GetElement(uci);
                    if (tap == null) continue;
                    sb.Append(
                        PCF_Taps.TapsWriter.WriteGenericTap(tap, uci, doc));
                }
            }

            return sb;
        }
        private StringBuilder WriteSpindle()
        {
            StringBuilder sb = new StringBuilder();
            if (SpindleDict.ContainsKey(Element.Id))
            {
                FamilyInstance sd = SpindleDict[Element.Id];
                Transform trf = sd.GetTransform();
                XYZ direction = trf.BasisZ;

                sb.AppendLine($"    SPINDLE-DIRECTION {mp.MapToCardinalDirection(direction)}");
            }
            return sb;
        }
    }
}
