using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_EP1_EP2_EP3_CPCONS : PcfPhysicalElement
    {
        public PCF_EP1_EP2_EP3_CPCONS(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EndWriter.WriteEP1(Element, Cons.Primary));
            sb.Append(EndWriter.WriteEP2(Element, Cons.Secondary));
            sb.Append(EndWriter.WriteEP3(Element, Cons.Tertiary));
            sb.Append(EndWriter.WriteCP(Cons.Primary, Cons.Secondary));

            return sb;
        }
    }
}
