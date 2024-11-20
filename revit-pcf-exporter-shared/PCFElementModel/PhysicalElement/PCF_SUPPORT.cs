using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_SUPPORT : PcfPhysicalElement
    {
        public PCF_SUPPORT(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(EndWriter.WriteCO((FamilyInstance)Element, Cons.Primary));

            return sb;
        }
    }
}
