using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Text;

using plst = PCF_Functions.Parameters;
using ew = PCF_Functions.EndWriter;

namespace PCF_Model
{
    internal class PCF_VIRTUAL_ISOSPLITPOINT : PcfVirtualElement
    {
        private Element Element;
        private Element Element2;
        private (Connector c1, Connector c2) Cons;

        public override ElementId ElementId => Element.Id;
        public override HashSet<Connector> AllConnectors => new HashSet<Connector>() { Cons.c1, Cons.c2 };

        public PCF_VIRTUAL_ISOSPLITPOINT((Connector c1, Connector c2) cons) : base("ISO-SPLIT-POINT")
        {
            this.Cons = cons;

            Element = cons.c1.Owner;
            Element2 = cons.c2.Owner;

            pcfData.Add(plst.PCF_ELEM_SKEY.Name, "WS");

            var centre = (cons.c1.Origin + cons.c2.Origin) / 2;

            endData.Add(($"CO-ORDS", $" {ew.PointStringMm(centre)}"));
        }
    }
}
