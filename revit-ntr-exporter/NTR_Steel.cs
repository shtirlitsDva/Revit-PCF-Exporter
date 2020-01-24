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

            foreach (var comb in result)
            {
                FindIntersectionsAndCreateNewElements(comb.fst, comb.snd, ASE_NewElementsList, sb);
            }

            return sb;
        }

        private void FindIntersectionsAndCreateNewElements
            (AnalyticalSteelElement fst, AnalyticalSteelElement snd, List<AnalyticalSteelElement> NewElementsList, StringBuilder sb)
        {
            fst.Curve.Intersect(snd.Curve, out IntersectionResultArray ira);
            if (ira != null)
            {
                sb.AppendLine(fst.Host.Id + " " + snd.Host.Id);
                foreach (IntersectionResult item in ira)
                {
                    //sb.AppendLine("Result " + i);
                    //sb.AppendLine("IsReadOnly " + item.IsReadOnly.ToString());
                    //sb.AppendLine("UVPoint " + item.UVPoint.ToString());
                    //sb.AppendLine("XYZPoint " + item.XYZPoint.ToString());

                    //Detect if the point is an end point
                    bool EqualsFstP1 = item.XYZPoint.Equalz(fst.P1, 1e-6);
                    bool EqualsFstP2 = item.XYZPoint.Equalz(fst.P2, 1e-6);
                    bool EqualsSndP1 = item.XYZPoint.Equalz(snd.P1, 1e-6);
                    bool EqualsSndP2 = item.XYZPoint.Equalz(snd.P2, 1e-6);
                    //sb.AppendLine(EqualsFstP1 + " " + EqualsFstP2 + " " + EqualsSndP1 + " " + EqualsSndP2);

                    EndPointsDetectionResult epdr = DetectEndPoints(EqualsFstP1, EqualsFstP2, EqualsSndP1, EqualsSndP2);
                    sb.AppendLine(epdr.ToString());
                    sb.AppendLine();

                    switch (epdr)
                    {
                        case EndPointsDetectionResult.NoEndPointsDetected:
                            break;
                        case EndPointsDetectionResult.FstEndPoint:
                            break;
                        case EndPointsDetectionResult.SndEndPoint:
                            break;
                        case EndPointsDetectionResult.BothElementsEndPoint:
                            break;
                        default:
                            break;
                    }
                }
            }
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

        public AnalyticalSteelElement(Document doc, AnalyticalModelStick ams)
        {
            Curve = ams.GetCurve();
            P1 = Curve.GetEndPoint(0);
            P2 = Curve.GetEndPoint(1);
            Host = doc.GetElement(ams.GetElementId());
        }
    }
}