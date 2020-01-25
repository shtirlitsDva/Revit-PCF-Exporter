using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using NTR_Functions;
using Shared;
using dw = NTR_Functions.DataWriter;

namespace NTR_Exporter
{
    internal class NTR_Steel
    {
        private ConfigurationData conf;
        private Document doc;

        public NTR_Steel(ConfigurationData conf, Document doc)
        {
            this.conf = conf;
            this.doc = doc;
        }

        internal StringBuilder Export()
        {
            StringBuilder sb = new StringBuilder();

            var AllAnalyticalModelSticks = Shared.Filter
                .GetElements<AnalyticalModelStick, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            List<AnalyticalSteelElement> ASE_OriginalList = new List<AnalyticalSteelElement>();
            List<AnalyticalSteelElement> ASE_NewElementsList = new List<AnalyticalSteelElement>();

            foreach (AnalyticalModelStick ams in AllAnalyticalModelSticks)
            {
                AnalyticalSteelElement ase = new AnalyticalSteelElement(doc, ams);
                ASE_OriginalList.Add(ase);
            }

            var result = ASE_OriginalList.SelectMany
                (
                    (fst, i) => ASE_OriginalList.Skip(i + 1).Select(snd => (fst, snd))
                );

            //Find all intersections in the structure system
            List<XYZ> AllIntersectionPoints = new List<XYZ>();
            foreach (var comb in result) FindIntersections(comb.fst, comb.snd, AllIntersectionPoints);

            //Create partial elements by finding intersection points on element.
            foreach (var ase in ASE_OriginalList) CreatePartialElements(ase, ASE_NewElementsList, AllIntersectionPoints);

            //Write steel data
            foreach (AnalyticalSteelElement ase in ASE_NewElementsList)
            {
                sb.Append("PROF ");
                sb.Append(dw.PointCoords("P1", ase.P1));
                sb.Append(dw.PointCoords("P2", ase.P2));
                //Hardcoded material until further notice
                sb.Append("MAT=S235JR ");


                sb.AppendLine();
            }

            #region Debug

            using (Transaction tx2 = new Transaction(doc))
            {
                tx2.Start("Debug placement of profiles.");
                foreach (var ase in ASE_NewElementsList)
                {
                    Shared.Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", ase.P1, ase.P2);

                }
                tx2.Commit();
            }

            #endregion
            return sb;
        }

        private void CreatePartialElements(AnalyticalSteelElement ASE, List<AnalyticalSteelElement> NewElements, List<XYZ> intersections)
        {
            throw new NotImplementedException();
        }

        private void FindIntersections(AnalyticalSteelElement fst, AnalyticalSteelElement snd, List<XYZ> list)
        {
            fst.Curve.Intersect(snd.Curve, out IntersectionResultArray ira);
            if (ira != null)
            {
                foreach (IntersectionResult intersection in ira)
                {
                    //Detect if the point is an end point
                    bool EqualsFstP1 = intersection.XYZPoint.Equalz(fst.P1, 1e-6);
                    bool EqualsFstP2 = intersection.XYZPoint.Equalz(fst.P2, 1e-6);
                    bool EqualsSndP1 = intersection.XYZPoint.Equalz(snd.P1, 1e-6);
                    bool EqualsSndP2 = intersection.XYZPoint.Equalz(snd.P2, 1e-6);

                    EndPointsDetectionResult epdr = DetectEndPoints(EqualsFstP1, EqualsFstP2, EqualsSndP1, EqualsSndP2);

                    switch (epdr)
                    {
                        case EndPointsDetectionResult.NoEndPointsDetected:
                            
                            
                            break;
                        case EndPointsDetectionResult.FstEndPoint:
                            break;
                        case EndPointsDetectionResult.SndEndPoint:
                            break;
                        case EndPointsDetectionResult.BothElementsEndPoint:
                            //Do nothing
                            break;
                        default:
                            break;
                    }

                    list.Add(intersection.XYZPoint);
                }
            }
        }

        internal AnalyticalSteelElement CreatePartASE(XYZ p1, XYZ p2, Element host)
        {
            AnalyticalSteelElement ASE = new AnalyticalSteelElement();
            ASE.P1 = p1; ASE.P2 = p2; ASE.Host = host;

            //This is not needed -> was used to find intersections
            //ASE.Curve = Line.CreateBound(p1, p2);

            return ASE;
        }

        internal EndPointsDetectionResult DetectEndPoints(bool FstP1, bool FstP2, bool SndP1, bool SndP2)
        {
            if ((FstP1 || FstP2) && (SndP1 || SndP2)) return EndPointsDetectionResult.BothElementsEndPoint;
            else if ((FstP1 || FstP2) && (!SndP1 || !SndP2)) return EndPointsDetectionResult.FstEndPoint;
            else if ((!FstP1 || !FstP2) && (SndP1 || SndP2)) return EndPointsDetectionResult.SndEndPoint;
            else return EndPointsDetectionResult.NoEndPointsDetected;
        }
    }

    internal enum EndPointsDetectionResult
    {
        NoEndPointsDetected,
        FstEndPoint,
        SndEndPoint,
        BothElementsEndPoint
    }

    internal class AnalyticalSteelElement
    {
        public XYZ P1;
        public XYZ P2;
        public Curve Curve;
        public Element Host;
        public bool IncludeInExport = true;

        public AnalyticalSteelElement() { }

        public AnalyticalSteelElement(Document doc, AnalyticalModelStick ams)
        {
            Curve = ams.GetCurve();
            P1 = Curve.GetEndPoint(0);
            P2 = Curve.GetEndPoint(1);
            Host = doc.GetElement(ams.GetElementId());
        }
    }
}