using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_FLANGE_BLIND : PcfPhysicalElement
    {
        private static Options options = new Options() { DetailLevel = ViewDetailLevel.Fine };

        public PCF_FLANGE_BLIND(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            Parameter pakning = Element.LookupParameter("Pakning");
            if (pakning != null && pakning.AsInteger() == 1)
                throw new Exception("Pakninger er ikke implementeret for blind flanger endnu!");

            sb.Append(EndWriter.WriteEP1(Element, Cons.Primary));

            XYZ endPointOriginFlangeBlind = Cons.Primary.Origin;
            double connectorSizeFlangeBlind = Cons.Primary.Radius;

            //Analyses the geometry to obtain a point opposite the main connector.
            //Extraction of the direction of the connector and reversing it
            XYZ reverseConnectorVector = -Cons.Primary.CoordinateSystem.BasisZ;
            Line detectorLine = Line.CreateBound(
                endPointOriginFlangeBlind, endPointOriginFlangeBlind + reverseConnectorVector * 10);
            //Begin geometry analysis
            GeometryElement geometryElement = ((FamilyInstance)Element).get_Geometry(options);

            //Prepare resulting point
            XYZ endPointAnalyzed = null;

            foreach (GeometryObject geometry in geometryElement)
            {
                if (geometry is GeometryInstance instance)
                {
                    foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                    {
                        Solid solid = instObj as Solid;
                        if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) { continue; }
                        // Get the faces
                        foreach (Face face in solid.Faces)
                        {
                            IntersectionResultArray results = null;
                            XYZ intersection = null;
                            SetComparisonResult result = face.Intersect(detectorLine, out results);
                            if (result == SetComparisonResult.Overlap)
                            {
                                intersection = results.get_Item(0).XYZPoint;
                                if (intersection.IsAlmostEqualTo(endPointOriginFlangeBlind) == false) endPointAnalyzed = intersection;
                            }
                        }
                    }
                }
            }

            sb.Append(EndWriter.WriteEP2(Element, endPointAnalyzed, connectorSizeFlangeBlind));

            return sb;
        }
    }
}
