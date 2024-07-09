using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_TEE : PCF_EP1_EP2_CPFI
    {
        public PCF_TEE(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(base.WriteSpecificData());

            sb.Append(EndWriter.WriteBP1(Element, Cons.Tertiary));

            return sb;
        }
    }
}
