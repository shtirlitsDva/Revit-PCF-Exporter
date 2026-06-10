using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_FLOOR_SYMBOL : PcfPhysicalElement
    {
        public PCF_FLOOR_SYMBOL(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(EW.WriteCO((FamilyInstance)Element));

            return sb;
        }
    }
}
