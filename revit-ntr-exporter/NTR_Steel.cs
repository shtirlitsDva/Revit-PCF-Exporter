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
        private Document doc;

        public NTR_Steel(Document doc)
        {
            this.doc = doc;
        }

        internal StringBuilder ExportSteel()
        {
            StringBuilder sb = new StringBuilder();

            //Collect in-model steel elements' analytical stick models
            var AllAnalyticalModelSticks = Shared.Filter
                .GetElements<AnalyticalModelStick, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            List<AnalyticalSteelElement> ASE_OriginalList = new List<AnalyticalSteelElement>();
            List<AnalyticalSteelElement> ASE_NewElementsList = new List<AnalyticalSteelElement>();

            foreach (AnalyticalModelStick ams in AllAnalyticalModelSticks)
            {
                AnalyticalSteelElement ase = new AnalyticalSteelElement(doc, ams);
                ASE_OriginalList.Add(ase);
            }

            #region Internal supports (supports on steels frames)
            //Create additional analytical stick elements for supports on steel frames
            var SteelSupports = Shared.Filter.GetElements<FamilyInstance, Guid>
                (doc, new Guid("f96a5688-8dbe-427d-aa62-f8744a6bc3ee"), "FRAME"); //.Cast<Element>();

            using (Transaction tx3 = new Transaction(doc))
            {
                tx3.Start("Dbg steel supports");
                foreach (FamilyInstance el in SteelSupports)
                {
                    Transform trf = el.GetTransform();
                    //trf = trf.Inverse;

                    XYZ Origin = new XYZ();
                    Origin = trf.OfPoint(Origin);

                    //case "Top":
                    XYZ Top = new XYZ(0, 0, 5);
                    Top = trf.OfPoint(Top);
                    //case "Bottom":
                    XYZ Bottom = new XYZ(0, 0, -5);
                    Bottom = trf.OfPoint(Bottom);
                    //case "Front":
                    XYZ Front = new XYZ(0, 5, 0);
                    Front = trf.OfPoint(Front);
                    //case "Back":
                    XYZ Back = new XYZ(0, -5, 0);
                    Back = trf.OfPoint(Back);
                    //case "Right":
                    XYZ Right = new XYZ(5, 0, 0);
                    Right = trf.OfPoint(Right);
                    //case "Left":
                    XYZ Left = new XYZ(-5, 0, 0);
                    Left = trf.OfPoint(Left);

                    Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Top);
                    Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Bottom);
                    Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Front);
                    Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Back);
                    //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Right);
                    //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Left);

                }

                tx3.Commit();
            }
            #endregion

            //Analyze steel structure finding all intersections and end points
            //And create analytical stick model with nodes at each intersection and end point
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

        internal StringBuilder ExportBoundaryConditions()
        {
            StringBuilder sb = new StringBuilder();

            var AllBoundaryConditions = Shared.Filter
                .GetElements<BoundaryConditions, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            //Hardcoded pinned support for now
            foreach (BoundaryConditions bc in AllBoundaryConditions)
            {
                sb.Append("FLAX");
                sb.Append(dw.PointCoords("PNAME", bc.Point));
                sb.AppendLine();
            }

            return sb;
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
