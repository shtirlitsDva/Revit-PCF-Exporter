using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_FLANGE : PcfPhysicalElement
    {
        public PCF_FLANGE(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(EW.WriteEP1(Element, Cons.Secondary));

            var pakning = Element.LookupParameter("Pakning");
            if (pakning != null && pakning.AsInteger() == 1)
            {
                XYZ dir = -Cons.Primary.CoordinateSystem.BasisZ.Normalize();
                XYZ modifiedPosition = Cons.Primary.Origin + dir * 1.5.MmToFt();
                
                sb.Append(EW.WriteEP2(Element, Cons.Primary, modifiedPosition));
            }
            else
            {
                sb.Append(EW.WriteEP2(Element, Cons.Primary));
            }

            return sb;
        }
    }
}
