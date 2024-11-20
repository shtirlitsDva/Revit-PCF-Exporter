using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_ELBOW : PCF_EP1_EP2_CPFI
    {
        public PCF_ELBOW(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(base.WriteSpecificData());

            sb.Append("    ANGLE ");

            Parameter par = Element.LookupParameter("Angle");
            if (par == null) par = Element.LookupParameter("angle");
            if (par == null) throw new Exception($"\"Angle\" parameter on elbow {Element.Id} does not exist or is named differently!");
            sb.Append((Conversion.RadianToDegree(par.AsDouble()) * 100).ToString("0"));
            sb.AppendLine();

            return sb;
        }
    }
}
