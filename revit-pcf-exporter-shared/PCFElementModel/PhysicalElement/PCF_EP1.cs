using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_EP1 : PcfPhysicalElement
    {
        public PCF_EP1(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(EndWriter.WriteEP1(Element, Cons.Primary));

            return sb;
        }
    }
}
