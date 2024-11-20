using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_REDUCER_ECCENTRIC : PCF_EP1_EP2
    {
        public PCF_REDUCER_ECCENTRIC(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.WriteSpecificData());

            //Temporary hack
            sb.AppendLine("    FLAT-DIRECTION DOWN");
            return sb;
        }
    }
}
