using Autodesk.Revit.DB;

using PCF_Functions;
using Shared;
using plst = PCF_Functions.Parameters;
using ew = PCF_Functions.EndWriter;
using mp = Shared.MepUtils;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_VIRTUAL_NN_GASKET : PcfVirtualElement
    {
        private Element Element;
        private Dictionary<string, string> specDescrDict = new Dictionary<string, string>() 
        {
            { "C02", "Pakning, Flat ring, EN 1514-1, 1,5 MM, Klinger PSM-B" },
            { "C03", "Pakning, Flat ring, EN 1514-1, 1,5 mm, Klinger PSM-B" },
            { "C08", "Pakning, Flat ring, EN 1514-1, 1,5 mm, Klinger C-4430" },
            { "S02", "Pakning, EN 1514-1-Type IBC-B, 1,5 mm, EPDM" }
        };
        public PCF_VIRTUAL_NN_GASKET(Element element) : base("GASKET") 
        { 
            this.Element = element;
            var ptype = Element.PipingSystemType(doc);
            var spec = plst.PCF_PIPL_SPEC.GetValue(Element);

            if (specDescrDict.ContainsKey(spec)) PCF_ELEM_DESCR = specDescrDict[spec];

            #region Write end point sizes
            Cons cons = new Cons(Element);
            XYZ cO = cons.Primary.Origin;

            XYZ dir = -cons.Primary.CoordinateSystem.BasisZ.Normalize();
            XYZ modifiedPosition = cons.Primary.Origin + dir * 1.5.MmToFt();

            double connectorSize = cons.Primary.Radius;

            WriteEndPoint(cO, connectorSize);
            WriteEndPoint(modifiedPosition, connectorSize); 
            #endregion
        }
        public override HashSet<Connector> AllConectors => mp.GetALLConnectorsFromElements(Element);
        private void WriteEndPoint(XYZ location, double size)
        {
            endData.Add(($"END-POINT", $" {ew.PointStringMm(location)} {Conversion.PipeSizeToMm(size)} FL"));
        }
    }
}
