using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_INSTRUMENT_DIAL : PCF_EP1
    {
        private static Options options = new Options() { DetailLevel = ViewDetailLevel.Fine };
        public PCF_INSTRUMENT_DIAL(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(base.WriteSpecificData());

            XYZ primConOrigin = Cons.Primary.Origin;

            //Analyses the geometry to obtain a point opposite the main connector.
            //Extraction of the direction of the connector and reversing it
            XYZ reverseConnectorVector = -Cons.Primary.CoordinateSystem.BasisZ;
            Line detectorLine = Line.CreateBound(primConOrigin, primConOrigin + reverseConnectorVector * 10);
            //Begin geometry analysis
            GeometryElement geometryElement = ((FamilyInstance)Element).get_Geometry(options);

            //Prepare resulting point
            XYZ endPointAnalyzed = null;

            foreach (GeometryObject geometry in geometryElement)
            {
                GeometryInstance instance = geometry as GeometryInstance;
                if (null == instance) continue;
                foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                {
                    Solid solid = instObj as Solid;
                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) continue;
                    foreach (Face face in solid.Faces)
                    {
                        IntersectionResultArray results = null;
                        XYZ intersection = null;
                        try
                        {
                            SetComparisonResult result = face.Intersect(detectorLine, out results);
                            if (result != SetComparisonResult.Overlap) continue;
                            intersection = results.get_Item(0).XYZPoint;
                            if (intersection.IsAlmostEqualTo(primConOrigin) == false) endPointAnalyzed = intersection;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }

            //If the point is still null after geometry intersection, it means the analysis failed
            //Create an artificial point
            if (endPointAnalyzed == null)
            {
                endPointAnalyzed = Cons.Primary.Origin + reverseConnectorVector * 2;
            }

            sb.Append(EndWriter.WriteCO(endPointAnalyzed));

            return sb;
        }
    }
}
