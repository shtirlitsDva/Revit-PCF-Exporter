using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_TEE : PCF_EP1_EP2_CPFI
    {
        public PCF_TEE(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(base.WriteSpecificData());

            sb.Append(EW.WriteBP1(Element, Cons.Tertiary));

            return sb;
        }
    }
}
