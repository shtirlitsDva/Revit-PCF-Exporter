using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_FLOOR_SYMBOL : PcfPhysicalElement
    {
        public PCF_FLOOR_SYMBOL(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(EndWriter.WriteCO((FamilyInstance)Element));

            return sb;
        }
    }
}
