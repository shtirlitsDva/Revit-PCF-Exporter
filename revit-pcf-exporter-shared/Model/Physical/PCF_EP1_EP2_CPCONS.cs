using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_EP1_EP2_CPCONS : PcfPhysicalElement
    {
        public PCF_EP1_EP2_CPCONS(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EW.WriteEP1(Element, Cons.Primary));
            sb.Append(EW.WriteEP2(Element, Cons.Secondary));
            sb.Append(EW.WriteCP(Cons.Primary, Cons.Secondary));

            return sb;
        }
    }
}
