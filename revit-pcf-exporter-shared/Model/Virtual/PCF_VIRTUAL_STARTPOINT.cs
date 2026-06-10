using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_VIRTUAL_STARTPOINT : PcfVirtualElement
    {
        private Element Element;
        public override ElementId ElementId => Element.Id;
        public override HashSet<Connector> AllConnectors => MepUtils.GetALLConnectorsFromElements(Element);
        public XYZ Location { get; set; }
        public PCF_VIRTUAL_STARTPOINT(Element element, XYZ location, ExportSession s) : base("STARTPOINT", s)
        {
            Element = element;
            Location = location;
        }
    }
}
