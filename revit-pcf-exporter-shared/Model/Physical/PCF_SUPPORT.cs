using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_SUPPORT : PcfPhysicalElement
    {
        public PCF_SUPPORT(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(EW.WriteCO((FamilyInstance)Element, Cons.Primary));

            return sb;
        }
    }
}
