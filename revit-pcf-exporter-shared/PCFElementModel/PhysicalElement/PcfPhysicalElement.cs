using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;

using PCF_Functions;

using plst = PCF_Functions.Parameters;
using pdef = PCF_Functions.ParameterDefinition;
using pd = PCF_Functions.ParameterData;

namespace PCF_Model
{
    public abstract class PcfPhysicalElement : IPcfElement
    {
        public Element Element { get; set; }

        #region Parameters read from Revit
        public pdef PCF_ELEM_TYPE = plst.PCF_ELEM_TYPE;
        
        #endregion

        #region Parameters defined programmatically
        public pdef PCF_ELEM_COMPID = plst.PCF_ELEM_COMPID;

        #endregion

        public PcfPhysicalElement(Element element) { 
            Element = element; }

        #region Writing to string
        public string WriteToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(PCF_ELEM_TYPE);
            sb.AppendLine("    COMPONENT-IDENTIFIER " + PCF_ELEM_COMPID);

            if (Specification == "EXISTING-INCLUDE")
            {
                sb.AppendLine("    STATUS DOTTED-UNDIMENSIONED");
                sb.AppendLine("    MATERIAL-LIST EXCLUDE");
            }

            return sb.ToString();
        }
        #endregion
    }
}
