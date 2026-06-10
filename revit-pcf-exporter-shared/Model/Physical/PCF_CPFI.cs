using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_CPFI : PcfPhysicalElement
    {
        public PCF_CPFI(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EW.WriteCP(Element as FamilyInstance));

            return sb;
        }
    }
}
