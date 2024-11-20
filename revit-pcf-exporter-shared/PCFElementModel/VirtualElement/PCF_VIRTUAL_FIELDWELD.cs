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
    internal class PCF_VIRTUAL_FIELDWELD : PcfVirtualElement
    {
        private Element Element;
        private Element Element2;
        private (Connector c1, Connector c2) Cons;

        public override ElementId ElementId => Element.Id;
        public override HashSet<Connector> AllConnectors => new HashSet<Connector>() { Cons.c1, Cons.c2 };

        public PCF_VIRTUAL_FIELDWELD((Connector c1, Connector c2) cons) : base("WELD")
        {
            this.Cons = cons;

            Element = cons.c1.Owner;
            Element2 = cons.c2.Owner;

            pcfData.Add(plst.PCF_ELEM_SKEY, "WS");
            pcfData.Add(plst.PCF_ELEM_CATEGORY, "ERECTION");
            pcfData.Add(plst.PCF_MAT_DESCR, "Field Weld");

            endData.Add($"    END-POINT {ew.PointStringMm(cons.c1.Origin)} {Conversion.PipeSizeToMm(cons.c1.Radius)} BW");
            endData.Add($"    END-POINT {ew.PointStringMm(cons.c2.Origin)} {Conversion.PipeSizeToMm(cons.c2.Radius)} BW");
        }
    }
}
