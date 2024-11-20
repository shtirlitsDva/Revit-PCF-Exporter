using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_FLANGE : PcfPhysicalElement
    {
        public PCF_FLANGE(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(EndWriter.WriteEP1(Element, Cons.Secondary));

            var pakning = Element.LookupParameter("Pakning");
            if (pakning != null && pakning.AsInteger() == 1)
            {
                XYZ dir = -Cons.Primary.CoordinateSystem.BasisZ.Normalize();
                XYZ modifiedPosition = Cons.Primary.Origin + dir * 1.5.MmToFt();
                
                sb.Append(EndWriter.WriteEP2(Element, Cons.Primary, modifiedPosition));
            }
            else
            {
                sb.Append(EndWriter.WriteEP2(Element, Cons.Primary));
            }

            return sb;
        }
    }
}
