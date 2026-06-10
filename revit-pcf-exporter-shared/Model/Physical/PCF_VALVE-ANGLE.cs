using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_VALVE_ANGLE : PCF_EP1_EP2
    {
        public PCF_VALVE_ANGLE(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(base.WriteSpecificData());

            //The centre point is obtained by creating an bound line from
            //primary connector and projecting the secondary point on the line.
            XYZ reverseConnectorVector = -Cons.Primary.CoordinateSystem.BasisZ;
            Line primaryLine = Line.CreateBound(
                Cons.Primary.Origin, Cons.Primary.Origin + reverseConnectorVector * 10);
            XYZ centrePoint = primaryLine.Project(Cons.Secondary.Origin).XYZPoint;

            sb.Append(EW.WriteCP(centrePoint));

            return sb;
        }
    }
}
