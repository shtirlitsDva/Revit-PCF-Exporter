using Autodesk.Revit.DB;

using PCF_Exporter;
using PCF_Functions;
using plst = PCF_Functions.Parameters;
using pdef = PCF_Functions.ParameterDefinition;

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PCF_Model
{
    internal abstract class PcfVirtualElement : IPcfElement
    {
        protected static Document doc => DocumentManager.Instance.Doc;
        protected Dictionary<pdef, string> pcfData = new Dictionary<pdef, string>();
        protected List<string> endData = new List<string>();
        protected string PCF_ELEM_TYPE { get; set; }
        public abstract HashSet<Connector> AllConnectors { get; }
        public abstract ElementId ElementId { get; }
        public string SystemAbbreviation =>
            doc.GetElement(ElementId).get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsValueString();

        public virtual bool ParticipateInMaterialTable => true;

        public PcfVirtualElement(string type) { PCF_ELEM_TYPE = type; }
        public string GetParameterValue(ParameterDefinition pdef)
        {
            if (pdef.Name == "PCF_ELEM_TYPE") return PCF_ELEM_TYPE;
            return pcfData.ContainsKey(pdef) ? pcfData[pdef] : null;
        }
        public void SetParameterValue(ParameterDefinition pdef, string value)
        {
            if (pdef.Name == "PCF_ELEM_TYPE") PCF_ELEM_TYPE = value;
            else pcfData[pdef] = value;
        }
        public StringBuilder ToPCFString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PCF_ELEM_TYPE);
            foreach (var item in pcfData.Where(x => x.Key != plst.PCF_MAT_DESCR))
            {
                sb.AppendLine($"    {item.Key.Keyword} {item.Value}");
            }
            foreach (var item in endData)
            {
                sb.AppendLine(item);
            }

            return sb;
        }
    }
}
