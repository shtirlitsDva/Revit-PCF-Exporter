using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

using PCF_Model;
using Shared;
using plst = PCF_Functions.Parameters;
using ew = PCF_Functions.EndWriter;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_VIRTUAL_STARTPOINT : PcfVirtualElement
    {
        private Element Element;
        public override ElementId ElementId => Element.Id;
        public override HashSet<Connector> AllConnectors => MepUtils.GetALLConnectorsFromElements(Element);
        public XYZ Location { get; set; }
        public PCF_VIRTUAL_STARTPOINT(Element element, XYZ location) : base("STARTPOINT")
        {
            Element = element;
            Location = location;
        }
    }
}
