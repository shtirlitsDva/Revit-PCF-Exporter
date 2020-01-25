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

            //Clean intersection points collection for duplicates
            AllIntersectionPoints = AllIntersectionPoints.Distinct(new XyzComparer()).ToList();

            //Create partial elements by finding intersection points on element.
            foreach (var ase in ASE_OriginalList) CreatePartialElements(ase, ASE_NewElementsList, AllIntersectionPoints);

            //Write steel data
            foreach (AnalyticalSteelElement ase in ASE_NewElementsList)
            {
                sb.Append("PROF ");
                sb.Append(dw.PointCoords("P1", ase.P1));
                sb.Append(dw.PointCoords("P2", ase.P2));
                //Hardcoded material until further notice
                sb.Append(" MAT=S235JR");
                sb.Append(dw.ParameterValue("TYP", BuiltInParameter.ELEM_TYPE_PARAM, ase.Host));
                sb.Append(" ACHSE=Y ");
                sb.Append(" RI='0,1,0' ");
                sb.Append(" LAST=STEEL ");
                sb.AppendLine();
            }

            #region Debug
            //using (Transaction tx2 = new Transaction(doc))
            //{
            //    tx2.Start("Debug placement of profiles.");
            //    foreach (var ase in ASE_NewElementsList)
            //    {
            //        Shared.Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", ase.P1, ase.P2);

            //    }
            //    tx2.Commit();
            //}
            #endregion
            return sb;
        }

        private void CreatePartialElements(AnalyticalSteelElement ASE, List<AnalyticalSteelElement> NewElements, List<XYZ> intersections)
        {
            var PointsOnLine = intersections.Where(x => FindPointsOnLine(x, ASE)).ToList();
            //Add elements' own points to collection of intersections
            //Also ensures something is in the collection if there are no intersections
            PointsOnLine.Add(ASE.P1);
            PointsOnLine.Add(ASE.P2);
            //Sort points geometrically to be able to create new elements sequentially
            var SortedPoints = PointsOnLine
                .OrderBy(p => p.X.Round(6)).ThenBy(p => p.Y.Round(6)).ThenBy(p => p.Z.Round(6))
                .ToList();
            //Create new elements sequentially
            for (int i = 0; i < SortedPoints.Count - 1; i++)
            {
                NewElements.Add(new AnalyticalSteelElement(SortedPoints[i], SortedPoints[i + 1], ASE.Host));
            }
        }

        private bool FindPointsOnLine(XYZ xyz, AnalyticalSteelElement ASE)
        {
            //Make sure no intersections coincident with end points pass the filter
            //Points at ends will be added 
            if (ASE.P1.Equalz(xyz, 1e-6)) return false;
            else if (ASE.P2.Equalz(xyz, 1e-6)) return false;
            return Math.Abs((ASE.P1.DistanceTo(xyz) + ASE.P2.DistanceTo(xyz)) - ASE.Curve.Length) < 1e-6;
        }

        private void FindIntersections(AnalyticalSteelElement fst, AnalyticalSteelElement snd, List<XYZ> list)
        {
            fst.Curve.Intersect(snd.Curve, out IntersectionResultArray ira);
            if (ira != null) foreach (IntersectionResult intersection in ira) list.Add(intersection.XYZPoint);
        }
    }

    internal class AnalyticalSteelElement
    {
        public XYZ P1;
        public XYZ P2;
        public Curve Curve;
        public Element Host;

        public AnalyticalSteelElement(XYZ p1, XYZ p2, Element host)
        {
            P1 = p1; P2 = p2; Host = host;
        }

        public AnalyticalSteelElement(Document doc, AnalyticalModelStick ams)
        {
            Curve = ams.GetCurve();
            P1 = Curve.GetEndPoint(0);
            P2 = Curve.GetEndPoint(1);
            Host = doc.GetElement(ams.GetElementId());
        }
    }
}
