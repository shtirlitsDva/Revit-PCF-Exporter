using Autodesk.Revit.DB;

using PCF_Exporter;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal abstract class PcfVirtualElement : IPcfElement
    {
        protected static Document doc => DocumentManager.Instance.Doc;
        protected Dictionary<string, string> pcfData = new Dictionary<string, string>();
        protected HashSet<(string, string)> endData = new HashSet<(string, string)>();
        protected string PCF_ELEM_TYPE { get; set; }
        protected string PCF_ELEM_DESCR { get; set; }
        public abstract HashSet<Connector> AllConectors { get; }
        public PcfVirtualElement(string type) { PCF_ELEM_TYPE = type; }
        public string GetParameterValue(ParameterDefinition pdef)
        {
            if (pdef.Name == "PCF_ELEM_DESCR") return PCF_ELEM_DESCR;
            else if (pdef.Name == "PCF_ELEM_TYPE") return PCF_ELEM_TYPE;
            return pcfData.ContainsKey(pdef.Name) ? pcfData[pdef.Name] : null;
        }
        public object GetParameterValue(string name)
        {
            if (name == "PCF_ELEM_DESCR") return PCF_ELEM_DESCR;
            else if (name == "PCF_ELEM_TYPE") return PCF_ELEM_TYPE;
            return pcfData.ContainsKey(name) ? pcfData[name] : null;
        }
        public void SetParameterValue(ParameterDefinition pdef, string value)
        {
            if (pdef.Name == "PCF_ELEM_DESCR") PCF_ELEM_DESCR = value;
            else if (pdef.Name == "PCF_ELEM_TYPE") PCF_ELEM_TYPE = value;
            else pcfData[pdef.Name] = value;
        }
        public StringBuilder ToPCFString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PCF_ELEM_TYPE);
            foreach (var item in pcfData)
            {
                sb.AppendLine($"    {plst.LPDict[item.Key].Keyword} {item.Value}");
            }
            foreach (var item in endData)
            {
                sb.AppendLine($"    {item.Item1} {item.Item2}");
            }

            return sb;
        }

    }
}
