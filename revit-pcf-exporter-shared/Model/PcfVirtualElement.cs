using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Model
{
    /// <summary>
    /// A PCF record with no Revit element of its own — derived from physical elements
    /// (gasket, field weld, iso split point, start point). Parameter values live in a
    /// local dictionary instead of Revit parameters.
    /// </summary>
    public abstract class PcfVirtualElement : IPcfElement
    {
        protected readonly ExportSession S;
        protected Document doc => S.Doc;
        protected Dictionary<pdef, string> pcfData = new Dictionary<pdef, string>();
        protected List<string> endData = new List<string>();
        protected string PCF_ELEM_TYPE { get; set; }

        public abstract HashSet<Connector> AllConnectors { get; }
        public abstract ElementId ElementId { get; }
        public string SystemAbbreviation =>
            doc.GetElement(ElementId)
               .get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM)
               .AsValueString();

        public virtual bool ParticipateInMaterialTable => true;

        protected PcfVirtualElement(string type, ExportSession session)
        {
            PCF_ELEM_TYPE = type;
            S = session;
        }

        public string GetParameterValue(pdef pdef)
        {
            if (pdef.Name == "PCF_ELEM_TYPE") return PCF_ELEM_TYPE;
            return pcfData.ContainsKey(pdef) ? pcfData[pdef] : null;
        }

        public void SetParameterValue(pdef pdef, string value)
        {
            if (pdef.Name == "PCF_ELEM_TYPE") PCF_ELEM_TYPE = value;
            else pcfData[pdef] = value;
        }

        public StringBuilder ToPCFString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(PCF_ELEM_TYPE);
            foreach (var item in pcfData.Where(x => x.Key != plst.PCF_MAT_DESCR))
                sb.AppendLine($"    {item.Key.Keyword} {item.Value}");
            foreach (var item in endData)
                sb.AppendLine(item);

            return sb;
        }
    }
}
