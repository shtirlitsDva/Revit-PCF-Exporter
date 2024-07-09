using Autodesk.Revit.DB;

using PCF_Exporter;
using PCF_Functions;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PcfVirtualElement : IPcfElement
    {
        protected static Document doc => DocumentManager.Instance.Doc;
        protected Dictionary<string, string> pcfData = new Dictionary<string, string>();
        public string PCF_ELEM_TYPE { get; }
        public PcfVirtualElement(string type) { PCF_ELEM_TYPE = type; }
        public string GetParameterValue(ParameterDefinition pdef) 
            => pcfData.ContainsKey(pdef.Name) ? pcfData[pdef.Name] : null;
        public object GetParameterValue(string name)
            => pcfData.ContainsKey(name) ? pcfData[name] : null;
        public void SetParameterValue(ParameterDefinition pdef, string value)
            => pcfData[pdef.Name] = value;

        public StringBuilder ToPCFString()
        {
            throw new NotImplementedException();
        }
    }
}
