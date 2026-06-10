using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_REDUCER_ECCENTRIC : PCF_EP1_EP2
    {
        public PCF_REDUCER_ECCENTRIC(Element element, ExportSession s) : base(element, s) { }
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
