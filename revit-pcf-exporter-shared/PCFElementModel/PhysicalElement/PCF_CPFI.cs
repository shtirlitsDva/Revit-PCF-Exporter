using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_CPFI : PcfPhysicalElement
    {
        public PCF_CPFI(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EndWriter.WriteCP(Element as FamilyInstance));

            return sb;
        }
    }
}
